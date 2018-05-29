using MySites.IServices;
using System;
using MySites.IRepositories.Interface;

namespace MySites.Services
{
    public class LoginService : ILoginService
    {
        private IUserRepositories userRepositories;
        public LoginService(IUserRepositories _userRepositories)
        {
            userRepositories = _userRepositories;
        }
        public void Test()
        {
            userRepositories.AddTest();
        }
    }
}
