using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Hl7Gateway.Services
{
    public class MllpListener
    {
        private readonly ILogger<MllpListener> _logger;
        private readonly IConfiguration _configuration;
        private readonly Hl7MessageProcessor _processor;
        private TcpListener? _listener;
        private bool _isRunning;

        // MLLP delimiters
        private const byte START_BLOCK = 0x0B; // VT (Vertical Tab)
        private const byte END_BLOCK = 0x1C;   // FS (File Separator)
        private const byte CARRIAGE_RETURN = 0x0D; // CR

        public MllpListener(
            ILogger<MllpListener> logger,
            IConfiguration configuration,
            Hl7MessageProcessor processor)
        {
            _logger = logger;
            _configuration = configuration;
            _processor = processor;
        }

        public async Task StartAsync()
        {
            var port = _configuration.GetValue<int>("Mllp:Port", 2575);
            var endpoint = new IPEndPoint(IPAddress.Any, port);
            _listener = new TcpListener(endpoint);
            _listener.Start();
            _isRunning = true;

            _logger.LogInformation("MLLP Listener iniciado en puerto {Port}", port);

            while (_isRunning)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClientAsync(client));
                }
                catch (ObjectDisposedException)
                {
                    // Listener fue cerrado
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error aceptando conexión cliente");
                }
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();
            _logger.LogInformation("MLLP Listener detenido");
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            var clientEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
            _logger.LogInformation("Cliente conectado: {Endpoint}", clientEndpoint);

            try
            {
                using (client)
                using (var stream = client.GetStream())
                {
                    var buffer = new byte[4096];
                    var messageBuffer = new List<byte>();

                    while (client.Connected)
                    {
                        var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        if (bytesRead == 0)
                            break;

                        messageBuffer.AddRange(buffer.Take(bytesRead));

                        // Buscar mensaje completo MLLP (0x0B ... 0x1C 0x0D)
                        var message = ExtractMllpMessage(messageBuffer);
                        if (message != null)
                        {
                            _logger.LogDebug("Mensaje MLLP recibido de {Endpoint}, longitud: {Length}", 
                                clientEndpoint, message.Length);

                            var response = await _processor.ProcessMessageAsync(message);
                            var mllpResponse = WrapMllpMessage(response);

                            await stream.WriteAsync(mllpResponse, 0, mllpResponse.Length);
                            await stream.FlushAsync();

                            _logger.LogDebug("ACK enviado a {Endpoint}", clientEndpoint);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error manejando cliente {Endpoint}", clientEndpoint);
            }
            finally
            {
                _logger.LogInformation("Cliente desconectado: {Endpoint}", clientEndpoint);
            }
        }

        private byte[]? ExtractMllpMessage(List<byte> buffer)
        {
            // Buscar START_BLOCK (0x0B)
            int startIndex = -1;
            for (int i = 0; i < buffer.Count; i++)
            {
                if (buffer[i] == START_BLOCK)
                {
                    startIndex = i;
                    break;
                }
            }

            if (startIndex == -1)
                return null;

            // Buscar END_BLOCK seguido de CARRIAGE_RETURN (0x1C 0x0D)
            int endIndex = -1;
            for (int i = startIndex + 1; i < buffer.Count - 1; i++)
            {
                if (buffer[i] == END_BLOCK && buffer[i + 1] == CARRIAGE_RETURN)
                {
                    endIndex = i;
                    break;
                }
            }

            if (endIndex == -1)
                return null;

            // Extraer mensaje (sin delimitadores MLLP)
            var messageLength = endIndex - startIndex - 1;
            var message = new byte[messageLength];
            Array.Copy(buffer.ToArray(), startIndex + 1, message, 0, messageLength);

            // Remover el mensaje procesado del buffer
            buffer.RemoveRange(0, endIndex + 2);

            return message;
        }

        private byte[] WrapMllpMessage(byte[] message)
        {
            var wrapped = new byte[message.Length + 3];
            wrapped[0] = START_BLOCK;
            Array.Copy(message, 0, wrapped, 1, message.Length);
            wrapped[wrapped.Length - 2] = END_BLOCK;
            wrapped[wrapped.Length - 1] = CARRIAGE_RETURN;
            return wrapped;
        }
    }
}

