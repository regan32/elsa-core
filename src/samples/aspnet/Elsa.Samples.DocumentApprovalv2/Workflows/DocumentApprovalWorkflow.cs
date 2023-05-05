using Elsa.Activities.Email;
using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Builders;
using Elsa.Activities.Temporal;
using NodaTime;
using Elsa.Activities.Primitives;
using Newtonsoft.Json.Linq;
using Elsa.Activities.Signaling.Extensions;


namespace Elsa.Samples.DocumentApprovalv2.Workflows
{
    public class DocumentApprovalWorkflow : IWorkflow
    {
        //{"Id": "Id", "Author": { "Name": "John", "Email": "john@gmail.com" },	"Body": "This is sample document."}

        public void Build(IWorkflowBuilder builder)
        {
            builder
                .WithDisplayName("Document Approval Workflow")
                .SetVariable("Document", context =>
                {
                    var a = context.Input as string;
                    return JObject.Parse(a);
                })
                .SendEmail(activity => activity
                    .WithSender("workflow@acme.com")
                    .WithRecipient("josh@acme.com")
                    .WithSubject(context => $"Document received from {context.GetVariable<dynamic>("Document")!.Author.Name}")
                    .WithBody(context =>
                    {
                        var document = context.GetVariable<dynamic>("Document")!;
                        var author = document!.Author;
                        context.GenerateSignalToken("Approve");
                        context.GenerateSignalToken("Reject");
                        return $"Document from {author.Name} received for review";
                    }))
                .Then<Fork>(activity => activity.WithBranches("Approve", "Reject", "Remind"), fork =>
                {
                    fork
                        .When("Approve")
                        .SignalReceived("Approve")
                        .SendEmail(activity => activity
                            .WithSender("workflow@acme.com")
                            .WithRecipient(context => context.GetVariable<dynamic>("Document")!.Author.Email)
                            .WithSubject(context => $"Document {context.GetVariable<dynamic>("Document")!.Id} Approved!")
                            .WithBody(context => $"Great job {context.GetVariable<dynamic>("Document")!.Author.Name}, that document is perfect."))
                        .ThenNamed("Join");

                    fork
                        .When("Reject")
                        .SignalReceived("Reject")
                        .SendEmail(activity => activity
                            .WithSender("workflow@acme.com")
                            .WithRecipient(context => context.GetVariable<dynamic>("Document")!.Author.Email)
                            .WithSubject(context => $"Document {context.GetVariable<dynamic>("Document")!.Id} Rejected")
                            .WithBody(context => $"Nice try {context.GetVariable<dynamic>("Document")!.Author.Name}, but that document needs work."))
                        .ThenNamed("Join");

                    fork
                        .When("Remind")
                        .Timer(Duration.FromSeconds(10)).WithName("Reminder")
                        .SendEmail(activity => activity
                                .WithSender("workflow@acme.com")
                                .WithRecipient("josh@acme.com")
                                .WithSubject(context => $"{context.GetVariable<dynamic>("Document")!.Author.Name} is waiting for your review!")
                                .WithBody(context =>
                                    $"Don't forget to review document {context.GetVariable<dynamic>("Document")!.Id}"))
                            .ThenNamed("Reminder");
                })
                .Add<Join>(join => join.WithMode(Join.JoinMode.WaitAny)).WithName("Join")
                .SendEmail(activity => activity
                    .WithSender("workflow@acme.com")
                    .WithRecipient("josh@acme.com")
                    .WithSubject(context => $"{context.GetVariable<dynamic>("Document")!.Author.Name} job done")
                    .WithBody(context =>
                        $"Job Done"));
        }
    }
}