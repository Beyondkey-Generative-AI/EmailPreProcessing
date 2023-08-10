using EmailManagementAPI.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace EmailManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    public class UserController : Controller
    {
        [HttpGet]
        [Route("Get")]
        public IEnumerable<UserInfo> Get()
        {
            using (var context = new EmailPreProcessingContext())
            {
                return context.UserInfos.ToList();
            }
        }

        [HttpGet]
        [Route("GetByUsername")]
        public UserInfo GetByUsername(string username)
        {
            using (var context = new EmailPreProcessingContext())
            {
                return context.UserInfos.FirstOrDefault(u => u.UserName == username);
            }
        }

       
    }
}