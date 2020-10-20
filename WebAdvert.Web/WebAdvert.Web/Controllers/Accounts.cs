using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.AspNetCore.Identity.Cognito;
using Amazon.Extensions.CognitoAuthentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebAdvert.Web.Models.Accounts;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebAdvert.Web.Controllers
{
    public class Accounts : Controller
    {
        #region Private Members
        private readonly SignInManager<CognitoUser> _signInManager;
        private readonly UserManager<CognitoUser> _userManager;
        private readonly CognitoUserPool _pool;

        #endregion


        #region Contractor
        public Accounts(SignInManager<CognitoUser> signInManager, UserManager<CognitoUser> userManager, CognitoUserPool pool)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _pool = pool;
        }
        #endregion

        [HttpGet]
        #region Methods
        public async Task<IActionResult> Signup(SignupModel model)
        {
            return View(model);
        }

        public async Task<IActionResult> Signup_index()
        {
            return View("Signup");
        }

        [HttpPost]
        public async Task<IActionResult> Signup_Post(SignupModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _pool.GetUser(model.Email);
                if (user == null)
                {
                    ModelState.AddModelError("UserExists", "This user you trying to create already exists");
                    return View(model);
                }

                user.Attributes.Add(CognitoAttributesConstants.Name, model.Email);
                var createdUser = await _userManager.CreateAsync(user, model.Password);
                if (createdUser.Succeeded)
                {
                    return RedirectToAction("Confirm_index");
                }
                else
                {
                    foreach (var error in createdUser.Errors)
                    {
                        ModelState.AddModelError(error.Code, error.Description);
                    }
                    return RedirectToAction("Signup", model);
                }
            }

            return RedirectToAction("Signup", model);
        }

        public async Task<IActionResult> Confirm_index()
        {
            return View("Confirm");
        }

        public async Task<IActionResult> Confirm(ConfirmModel model)
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Confirm_Post(ConfirmModel model)
        {
            //Check if model i valid
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email).ConfigureAwait(false);
                if (user == null)
                {
                    ModelState.AddModelError("NotFound", "No user was found for this email address");
                    return View(model);
                }


                try
                {
                    var result = await (_userManager as CognitoUserManager<CognitoUser>).ConfirmSignUpAsync(user, model.Code, true).ConfigureAwait(false);
                    if (result.Succeeded)
                    {
                        return RedirectToAction("index", "Home");
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(error.Code, error.Description);
                            return View(model);
                        }
                    }
                }
                catch (Exception ex)
                {
                    string error = ex.Message.ToString();
                }

            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> LoginIndex()
        {
            return View("Login");
        }


        [HttpPost]
        [ActionName("Login")]
        public async Task<IActionResult> LoginPost(LoginModel model)
        {
            if(ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);
                if(result.Succeeded)
                {
                    return RedirectToAction("index", "home");
                }
                else
                {
                    ModelState.AddModelError("LoginFailed", "Login failed: Email address or password did not match");
                    return View("Login", model);
                }
            }
            else
            {
                return View("Login", model);
            }
        }

        #endregion
    }
}
