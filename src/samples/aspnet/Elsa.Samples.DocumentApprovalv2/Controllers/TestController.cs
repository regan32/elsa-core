using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Signaling.Services;
using Elsa.Samples.DocumentApprovalv2.Workflows;
using Elsa.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Elsa.Samples.DocumentApprovalv2.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        private readonly IBuildsAndStartsWorkflow workflowRunner;
        private readonly ILogger<TestController> _logger;
        private readonly ISignaler signaler;

        public TestController(IBuildsAndStartsWorkflow workflowRunner, ILogger<TestController> logger, ISignaler signaler)
        {
            this.workflowRunner = workflowRunner;
            _logger = logger;
            this.signaler = signaler;
        }

        [HttpPut("workflow", Name = "StartWorkflow")]
        public async Task StartWorkflow(string document = "{\"Id\": \"Id\", \"Author\": { \"Name\": \"John\", \"Email\": \"john@gmail.com\" },\t\"Body\": \"This is sample document.\"}")
        {
            await workflowRunner.BuildAndStartWorkflowAsync<DocumentApprovalWorkflow>(input: new Elsa.Models.WorkflowInput() { Input = document });
        }

        [HttpPut("approve", Name = "SignalApprove")]
        public async Task Approve([Optional] string id)
        {
            await signaler.TriggerSignalAsync("Approve", workflowInstanceId: id);
        }

        [HttpPut("reject", Name = "SignalReject")]
        public async Task Reject([Optional] string id)
        {
            await signaler.TriggerSignalAsync("Reject", workflowInstanceId: id);
        }
    }
}