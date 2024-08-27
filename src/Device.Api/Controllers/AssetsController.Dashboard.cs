using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Device.Application.Analytic.Query;
namespace Device.Api.Controller
{
    public partial  class AssetsController : ControllerBase
    {
        [HttpPost("regression")]
        public async Task<IActionResult> GetAssetAttributesRegression([FromBody] GetAssetAttributeRegressionData command, [FromQuery] int timeout = 5)
        {
            command.TimeoutInSecond = timeout;
            var response = await _mediator.Send(command);
            // using high performance json serializer
            byte[] jsonUtf8Bytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(response, _jsonSetting);
            return File(jsonUtf8Bytes, "application/json");
        }
        [HttpPost("manual-regression")]
        public async Task<IActionResult> ManualRegression([FromBody] ManualRegressionData command, [FromQuery] int timeout = 5)
        {
            command.TimeoutInSecond = timeout;
            var response = await _mediator.Send(command);
            // using high performance json serializer
            byte[] jsonUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(response, _jsonSetting);
            return File(jsonUtf8Bytes, "application/json");
        }
        [HttpPost("statistics")]
        public async Task<IActionResult> GetAssetAttributesStatistics([FromBody] GetAssetAttributeStatisticsData command)
        {
            var response = await _mediator.Send(command);
            // using high performance json serializer
            byte[] jsonUtf8Bytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(response, _jsonSetting);
            return File(jsonUtf8Bytes, "application/json");
        }

    }
}