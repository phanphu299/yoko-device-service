using System;
using System.Collections.Generic;

namespace Device.Application.Models
{
    public class UserInfoDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string Upn { get; set; }
        public IEnumerable<string> ObjectRightShorts { get; set; }
        public IEnumerable<string> RightShorts { get; set; }
    }
}
