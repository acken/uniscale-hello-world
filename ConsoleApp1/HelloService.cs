using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uniscale.Core;
using Uniscale.Designtime;
using SveinArnesCompany.HelloService;
using SveinArnesCompany.HelloService_1_0;
using SveinArnesCompany.Notifications_1_0.Functionality.ServiceToService.Notifications.SendNotifications;
using SveinArnesCompany.Notifications.Notifications;
using SveinArnesCompany.HelloService.Greetings;

namespace ConsoleApp1 {
    internal class HelloService {
        public static async Task<PlatformSession> Get(PlatformSession notificationsSession) {
            var sessionBuilder = Platform.Builder();
            // Prepare for outgoing calls from the service
            var forwardingSessionBuilder = await sessionBuilder
                .ForwardingSessionBuilder(Guid.Parse("cef03bda-9f72-48ac-8206-6c632b0ea77d"))
                .WithInterceptors(i => i
                    .InterceptPattern(SveinArnesCompany.Notifications.Patterns.Pattern, async (input, ctx) => {
                        var requestJson = GatewayRequest.From(input, ctx).ToJson();
                        // So what if we wanted to move the service out as it's own HTTP service?
                        // POST https://our-greetings-server.com/api/service-to-module/{featureId}
                        //      requestJston
                        //
                        //      responseJson <-
                        //      var response = Result<object>.FromJson(responseJson);
                        var response = await notificationsSession.AcceptGatewayRequest(requestJson);
                        return response;
                    }))
                .Build();

            // Handle incoming calls for the service
            return await sessionBuilder
                .WithInterceptors(i => i
                    .InterceptRequest(
                        Patterns.Greetings.GetHelloWorld.AllRequestUsages,
                        Patterns.Greetings.GetHelloWorld.Handle((input, ctx) => "Hello, World!")
                    )
                    .InterceptRequest(
                        Patterns.Greetings.GetPersonalGreeting.AllRequestUsages,
                        Patterns.Greetings.GetPersonalGreeting.Handle(async (input, ctx) => {
                            // Validation
                            if (String.IsNullOrEmpty(input))
                                return Result<string>.BadRequest(ErrorCodes.Greetings.PersonToGreetIsRequired, "Invalid input for GetPersonalGreeting");

                            // Request to the notification service
                            var message = "Hello to you " + input;
                            var dispatcher = await forwardingSessionBuilder.ForTransaction(ctx.TransactionId);
                            var sendResult = await dispatcher.Request(NewNotification.With(new NotificationValues {
                                Title = "Personal greeting",
                                Description = message,
                                Name = input
                            }));
                            if (!sendResult.Success)
                                return Result<string>.InternalServerError(
                                    ErrorCodes.Greetings.NotificationServiceFailure, 
                                    "Failed when calling the notification services: NewNotification");

                            // Response
                            return Result.Ok(message);
                        }
                    ))
                    .InterceptRequest(
                        Patterns.Greetings.GetPersonalGreetingBasedOnMood.AllRequestUsages,
                        Patterns.Greetings.GetPersonalGreetingBasedOnMood.Handle(async (input, ctx) => {
                            // Validation
                            if (String.IsNullOrEmpty(input.Name))
                                return Result<GetPersonalGreetingBasedOnMoodOutput>.BadRequest(ErrorCodes.Greetings.PersonToGreetIsRequired, "Invalid input for GetPersonalGreetingBasedOnMood");
                            var dispatcher = await forwardingSessionBuilder
                                .ForTransaction(ctx.TransactionId);
                            var moodValidation = await dispatcher.ValidateTerminologyCode(Terminologies.Greetings.Mood.Id, input.Mood);
                            if (!moodValidation.Success)
                                return moodValidation.AsEmpty<GetPersonalGreetingBasedOnMoodOutput>();

                            // Render the message
                            GetPersonalGreetingBasedOnMoodOutput message = null;
                            switch (input.Mood) {
                                case "Happy":
                                    message = new GetPersonalGreetingBasedOnMoodOutput {
                                        Mood = input.Mood,
                                        Greeting = "A happy day to you " + input.Name
                                    };
                                    break;
                                case "Sad":
                                    message = new GetPersonalGreetingBasedOnMoodOutput {
                                        Mood = input.Mood,
                                        Greeting = "We are sorry to hear you are sad " + input.Name + ". Remember that you are awesome!"
                                    };
                                    break;
                                default:
                                    return Result<GetPersonalGreetingBasedOnMoodOutput>.InternalServerError(new Error(
                                        "unexpected",
                                        new ErrorDetails {
                                            TechnicalError = "The logic around the mood " + input.Mood + " is not supported by the backend",
                                            UserError = "Something unexpected happened. Please contact support."
                                        },
                                        new List<Error>(),
                                        null));
                            }

                            // Request to the notification service
                            var sendResult = await dispatcher.Request(NewNotification.With(new NotificationValues {
                                Title = "Personal moody greeting",
                                Description = message.Greeting,
                                Name = input.Name
                            }));
                            if (!sendResult.Success)
                                return Result<GetPersonalGreetingBasedOnMoodOutput>.InternalServerError(
                                    ErrorCodes.Greetings.NotificationServiceFailure,
                                    "Failed when calling the notification services: NewNotification");

                            // Response
                            return Result.Ok(message);
                         }
                    )))
                .Build();
        }
    }
}
