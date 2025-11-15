using System.Net.Sockets;
using System.Text;

namespace Hl7Sender
{
    class Program
    {
        private const byte START_BLOCK = 0x0B; // VT
        private const byte END_BLOCK = 0x1C;   // FS
        private const byte CARRIAGE_RETURN = 0x0D; // CR

        static async Task Main(string[] args)
        {
            var host = args.Length > 0 ? args[0] : "localhost";
            var port = args.Length > 1 ? int.Parse(args[1]) : 2575;

            Console.WriteLine($"Conectando a {host}:{port}...");

            // Mensaje HL7 v2 ORM^O01 de ejemplo
            var hl7Message = @"MSH|^~\&|SENDING_APP|SENDING_FACILITY|RECEIVING_APP|RECEIVING_FACILITY|20250114120000||ORM^O01|MSG001|P|2.3
PID|||12345678^^^DNI||GARCIA^JUAN^MARIA||19850115|M|||123 CALLE PRINCIPAL^^CIUDAD^PROVINCIA^12345||5551234567|||||||||||||||||
PV1||I|EMERGENCY^A1^01||||12345^DOCTOR^JOHN^MD|||||||||||V123456|||||||||||||||||||||||20250114120000
ORC|NW|ORD001|||CM|N||||20250114120000|^DOCTOR^JOHN^MD|12345^DOCTOR^JOHN^MD||||||
OBR|1|ORD001||LAB001^LABORATORIO COMPLETO^L|||20250114120000|||||||||^DOCTOR^JOHN^MD||||||20250114120000|||F";

            try
            {
                using (var client = new TcpClient(host, port))
                using (var stream = client.GetStream())
                {
                    Console.WriteLine("Conectado. Enviando mensaje HL7...");
                    Console.WriteLine("\nMensaje a enviar:");
                    Console.WriteLine(hl7Message);
                    Console.WriteLine();

                    // Envolver mensaje en MLLP
                    var messageBytes = Encoding.UTF8.GetBytes(hl7Message);
                    var mllpMessage = new byte[messageBytes.Length + 3];
                    mllpMessage[0] = START_BLOCK;
                    Array.Copy(messageBytes, 0, mllpMessage, 1, messageBytes.Length);
                    mllpMessage[mllpMessage.Length - 2] = END_BLOCK;
                    mllpMessage[mllpMessage.Length - 1] = CARRIAGE_RETURN;

                    // Enviar mensaje
                    await stream.WriteAsync(mllpMessage, 0, mllpMessage.Length);
                    await stream.FlushAsync();

                    Console.WriteLine("Mensaje enviado. Esperando ACK...");

                    // Leer respuesta ACK
                    var buffer = new byte[4096];
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    
                    if (bytesRead > 0)
                    {
                        // Desenvolver MLLP
                        var ackBytes = new byte[bytesRead - 3];
                        Array.Copy(buffer, 1, ackBytes, 0, ackBytes.Length);
                        var ackMessage = Encoding.UTF8.GetString(ackBytes);
                        
                        Console.WriteLine("\nACK recibido:");
                        Console.WriteLine(ackMessage);
                    }
                    else
                    {
                        Console.WriteLine("No se recibió respuesta.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Environment.Exit(1);
            }

            Console.WriteLine("\nPresiona cualquier tecla para salir...");
            Console.ReadKey();
        }
    }
}
