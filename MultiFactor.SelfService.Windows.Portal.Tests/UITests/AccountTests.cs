using OpenQA.Selenium;
using System;
using System.Linq;
using Xunit;

namespace MultiFactor.SelfService.Windows.Portal.Tests.UITests
{
    public class AccountTests : AutomatedUITests
    {
        [Fact]
        public void Login_Title_Success()
        {
            _driver.Navigate().GoToUrl("http://localhost:49689");
            Assert.StartsWith("Портал двухфакторной аутентификации", _driver.Title);
        }
        
        [Fact]
        public void Login_ValidCreds_Success()
        {
            _driver.Navigate().GoToUrl("http://localhost:49689");

            var login = _driver.FindElement(By.Id("UserName"));
            var password = _driver.FindElement(By.Id("Password"));
            var submit = _driver.FindElement(By.Id("submit"));

            submit.Click();

            var cookie = _driver.Manage().Cookies.GetCookieNamed("multifactor");
            Assert.NotNull(cookie);

            var form = _driver.FindElement(By.Id("remove-authenticator-form"));
            Assert.NotNull(form);
        }
    }
}
