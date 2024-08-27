using Device.Application.Constant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Device.Api.Controller
{
    [Route("dev/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "oidc")]
    public class DataTypesController : ControllerBase
    {
        public DataTypesController()
        {
        }

        [HttpPost("search")]
        public IActionResult SearchAsync()
        {
            //var response = await _mediator.Send(command);
            return Ok(new
            {
                totalCount = 4,
                totalPage = 1,
                pageSize = 20,
                pageIndex = 0,
                data = new[] {
                new { id = DataTypeConstants.TYPE_TEXT, name = DataTypeConstants.TYPE_TEXT },
                new { id =  DataTypeConstants.TYPE_BOOLEAN, name = DataTypeConstants.TYPE_BOOLEAN },
                new { id = DataTypeConstants.TYPE_INTEGER, name = DataTypeConstants.TYPE_INTEGER },
                new { id = DataTypeConstants.TYPE_DOUBLE,name =  DataTypeConstants.TYPE_DOUBLE }, // change to double since the customer allow the rounding of IoT data
                new { id = DataTypeConstants.TYPE_DATETIME, name = DataTypeConstants.TYPE_DATETIME}
                }
            });
        }
        [HttpPost("functionblock")]
        public IActionResult SearchFunctionBlockDataTypeAsync()
        {
            return Ok(new
            {
                totalCount = 6,
                totalPage = 1,
                pageSize = 20,
                pageIndex = 0,
                data = new[]
                {
                    new { id = BindingDataTypeIdConstants.TYPE_ASSET_ATTRIBUTE,name =  BindingDataTypeNameConstants.NAME_ASSET_ATTRIBUTE },
                    new { id = BindingDataTypeIdConstants.TYPE_ASSET_TABLE,name =  BindingDataTypeNameConstants.NAME_ASSET_TABLE },
                    new { id = BindingDataTypeIdConstants.TYPE_DOUBLE, name =  BindingDataTypeNameConstants.NAME_DOUBLE },
                    new { id = BindingDataTypeIdConstants.TYPE_INTEGER, name = BindingDataTypeNameConstants.NAME_INTEGER },
                    new { id = BindingDataTypeIdConstants.TYPE_BOOLEAN, name = BindingDataTypeNameConstants.NAME_BOOLEAN },
                    new { id = BindingDataTypeIdConstants.TYPE_TEXT, name = BindingDataTypeNameConstants.NAME_TEXT },
                    new { id = BindingDataTypeIdConstants.TYPE_DATETIME, name = BindingDataTypeNameConstants.NAME_DATETIME }
                }
            });
        }
    }
}
