// See https://aka.ms/new-console-template for more information

using Uniscale.Core;
using Uniscale.Designtime;
using SveinArnesCompany.HelloService_1_0.Functionality.ServiceToModule.Greetings.OutputGreetings;
using SveinArnesCompany.Notifications_1_0.Functionality.ServiceToModule.Notifications.NotificationOverview;
using ConsoleApp1;
using SveinArnesCompany.HelloService.Greetings;

var notificationSession = await NotificationService.Get();
var helloServiceSession = await HelloService.Get(notificationSession);


var clientSession = await Platform.Builder()
    .WithInterceptors(i => i
        .InterceptPattern(SveinArnesCompany.HelloService.Patterns.Pattern, async (input, ctx) => {
            var requestJson = GatewayRequest.From(input, ctx).ToJson();
            // So what if we wanted to move the service out as it's own HTTP service?
            // POST https://our-greetings-server.com/api/service-to-module/{featureId}
            //      requestJston
            //
            //      responseJson <-
            //      var response = Result<object>.FromJson(responseJson);
            var response = await helloServiceSession.AcceptGatewayRequest(requestJson);
            return response;
        })
        .InterceptPattern(SveinArnesCompany.Notifications.Patterns.Pattern, async (input, ctx) => {
            var requestJson = GatewayRequest.From(input, ctx).ToJson();
            // So what if we wanted to move the service out as it's own HTTP service?
            // POST https://our-greetings-server.com/api/service-to-module/{featureId}
            //      requestJston
            //
            //      responseJson <-
            //      var response = Result<object>.FromJson(responseJson);
            var response = await notificationSession.AcceptGatewayRequest(requestJson);
            return response;
        }))
    .Build();

var dispatcher = clientSession.AsSolution(Guid.Parse("e9f3a3a0-514e-414c-bd72-c44558275c12"));

// Say hello to the world
Console.WriteLine("Saying hello world:");
var helloWorld = await dispatcher.Request(GetHelloWorld.With(new SveinArnesCompany.HelloService.Greetings.Empty()));
if (helloWorld.Success)
    Console.WriteLine(helloWorld.Value);
else
    Console.WriteLine(helloWorld.Error.ToLongString());
Console.WriteLine();

Console.WriteLine("Personal greeting:");
var helloToSomeone = await dispatcher.Request(GetPersonalGreeting.With("John"));
if (helloToSomeone.Success)
    Console.WriteLine(helloToSomeone.Value);
else
    Console.WriteLine(helloToSomeone.Error.Details.UserError);
Console.WriteLine();

Console.WriteLine("Personal moody greeting:");
var moodyHello = await dispatcher.Request(GetPersonalGreetingBasedOnMood.With(new GetPersonalGreetingBasedOnMoodInput {
    Name = "John",
    Mood = "Happy"
}));
if (moodyHello.Success)
    Console.WriteLine(moodyHello.Value.Greeting);
else
    Console.WriteLine(moodyHello.Error.ToLongString());
Console.WriteLine("");

Console.WriteLine("List of notifications");
var messageList = await dispatcher.Request(GetMessageList.With(new SveinArnesCompany.Notifications.Notifications.Empty()));
if (messageList.Success) {
    foreach (var msg in messageList.Value)
        Console.WriteLine("\t" + msg.Description + " (" + msg.Title + ")");
} else {
    Console.WriteLine(messageList.Error.ToLongString());
}
