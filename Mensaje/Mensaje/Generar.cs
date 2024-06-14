using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;

public class Ticket
{
    public string TicketId { get; set; }
    public string AlarmEventId { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
}
public class AlarmEvent
{
    public string Id { get; set; }
    public string Description { get; set; }
    public DateTime Timestamp { get; set; }
}

class Generar
{
    const string connectionString = "Endpoint=sb://pagoproveedores.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=W+CODu7cj4zIvhWsQC+4QQF9mn6vZEVus+ASbGY8Jq4=";
    const string pagosQueueName = "pagos";
    const string ticketsQueueName = "tickets";
    
   static async Task Main(string[] args)
    {
        await ProcessAlarmEventsAsync();
    }

    static async Task ProcessAlarmEventsAsync()
    {
        ServiceBusClient client = new ServiceBusClient(connectionString);
        ServiceBusReceiver receiver = client.CreateReceiver(pagosQueueName);

        while (true)
        {
            ServiceBusReceivedMessage receivedMessage = await receiver.ReceiveMessageAsync();

            if (receivedMessage != null)
            {
                string messageBody = receivedMessage.Body.ToString();
                AlarmEvent alarmEvent = JsonConvert.DeserializeObject<AlarmEvent>(messageBody);

                // Crear un ticket basado en el alarmEvent
                Ticket ticket = new Ticket
                {
                    TicketId = Guid.NewGuid().ToString(),
                    AlarmEventId = alarmEvent.Id,
                    Description = alarmEvent.Description,
                    CreatedAt = DateTime.UtcNow
                };
//Enviar el TicketId a la cola tickets
                await SendTicketIdAsync(ticket);

                Console.WriteLine($"Ticket created for alarm event: {alarmEvent.Id}, Ticket ID: {ticket.TicketId}");

                // Completar el mensaje de la cola incidents
                await receiver.CompleteMessageAsync(receivedMessage);
            }
        }

        await client.DisposeAsync();
    }

    static async Task SendTicketIdAsync(Ticket ticket)
    {
        ServiceBusClient client = new ServiceBusClient(connectionString);
        ServiceBusSender sender = client.CreateSender(ticketsQueueName);

        string messageBody = JsonConvert.SerializeObject(new { TicketId = ticket.TicketId });
        ServiceBusMessage message = new ServiceBusMessage(messageBody);

        await sender.SendMessageAsync(message);
        Console.WriteLine($"Ticket ID sent: {ticket.TicketId}");

        await client.DisposeAsync();
    }
}
