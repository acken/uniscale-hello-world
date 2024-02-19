using SveinArnesCompany.Notifications.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uniscale.Core;
using Uniscale.Designtime;
using SveinArnesCompany.Notifications;

namespace ConsoleApp1 {
    internal class NotificationService {
        public static async Task<PlatformSession> Get() {
            var notifications = new List<NotificationFull>();

            // Handle incoming calls for the service
            return await Platform.Builder()
                .WithInterceptors(i => i
                    .InterceptMessage(
                        Patterns.Notifications.NewNotification.AllMessageUsages,
                        Patterns.Notifications.NewNotification.Handle((input, ctx) => {
                            notifications.Add(new NotificationFull {
                                NotificationIdentifier = Guid.NewGuid(),
                                Title = input.Title,
                                Description = input.Description,
                                Name = input.Name
                            });
                            //return Result.InternalServerError("db_down", "Cannot communicate with the database");
                        })
                    )
                    .InterceptRequest(
                        Patterns.Notifications.GetMessageList.AllRequestUsages,
                        Patterns.Notifications.GetMessageList.Handle((input, ctx) => {
                            return notifications;
                        })
                    ))
                .Build();
        }
    }
}
