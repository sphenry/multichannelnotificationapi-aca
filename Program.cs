using Azure.Communication.Email; 
using Azure.Communication.Sms; 
using Azure.Communication.Messages; 
using Azure;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Build configuration
var configuration = builder.Configuration;

// Access settings
var connectionString = configuration.GetSection("AzureSettings")["COMMUNICATION_SERVICES_CONNECTION_STRING"];
var senderEmail = configuration.GetSection("AzureSettings")["SENDER_EMAIL_ADDRESS"];
var senderSms = configuration.GetSection("AzureSettings")["SENDER_PHONE_NUMBER"];
var senderWhatsApp = configuration.GetSection("AzureSettings")["WHATSAPP_NUMBER"];

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var _emailClient = new EmailClient(connectionString);
var _smsClient = new SmsClient(connectionString);
var _messagesClient = new NotificationMessagesClient(connectionString);

app.MapPost("/sendEmail", async (EmailRequest request) =>
{
    EmailSendOperation emailSendOperation = await _emailClient.SendAsync(
        Azure.WaitUntil.Completed,
        senderEmail,
        request.Recipient,
        request.Subject,
        request.HtmlContent
    );

    return Results.Ok("Email sent successfully");
})
.WithName("SendEmail")
.WithOpenApi();

app.MapPost("/sendSms", async (SmsRequest request) =>
{
    SmsSendResult smsSendResult = await _smsClient.SendAsync(
        senderSms,
        request.PhoneNumber,
        request.Message
    );

    return Results.Ok("SMS sent successfully");
})
.WithName("SendSms")
.WithOpenApi();

app.MapPost("/sendWhatsAppMessage", async (WhatsAppRequest request) =>
{
    List<string> recipientList = new List<string> { request.PhoneNumber };
                List<MessageTemplateText> values = request.TemplateParameters
                    .Select((parameter, index) => new MessageTemplateText($"value{index + 1}", parameter))
                    .ToList();
                MessageTemplateWhatsAppBindings bindings = new MessageTemplateWhatsAppBindings(
                    body: values.Select(value => value.Name).ToList()
                );
                MessageTemplate template = new MessageTemplate(request.TemplateName, request.TemplateLanguage, values, bindings);
                SendMessageOptions sendTemplateMessageOptions = new SendMessageOptions(senderWhatsApp, recipientList, template);
                Response<SendMessageResult> templateResponse = await _messagesClient.SendMessageAsync(sendTemplateMessageOptions);


    return Results.Ok("WhatsApp sent successfully");
})
.WithName("SendWhatsAppMessage")
.WithOpenApi();

app.Run();

public class EmailRequest
{
    public string Subject { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
}

public class SmsRequest
{
    public string Message { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}

public class WhatsAppRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string TemplateName { get; set; } = "appointment_reminder";
    public string TemplateLanguage { get; set; } = "en";
    public List<string> TemplateParameters { get; set; } = new List<string>();
}

