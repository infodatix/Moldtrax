﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Diagnostics;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.Security;
using ActiveDirectoryWebApp.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Moldtrax.Models;
using Moldtrax.Providers;

namespace Moldtrax.Controllers
{
    public class AccountController : Controller
    {
        private MoldtraxDbContext db = new MoldtraxDbContext();

        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        public AccountController()
        {

        }

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        private void WriteEventLog(string msg)
        {
            if (!EventLog.SourceExists("moldtrax"))
            {
                EventLog.CreateEventSource("moldtrax", "applicationlog");
            }
            EventLog myLog = new EventLog();
            myLog.Source = "moldtrax";
            myLog.WriteEntry(msg);
        }

        public ActionResult OAuthenticationLogin()
        {
            //WriteEventLog("OAuthenticationLogin");
            StringBuilder sb = new StringBuilder();
            var user1 = System.Environment.GetEnvironmentVariable("UserName");// User.Identity.Name;//Environment.UserName;user

            var user2 = WindowsIdentity.GetCurrent().Name;
            user2 = user2.Substring(user2.LastIndexOf("\\")+1, user2.Length - user2.LastIndexOf("\\")-1);
            WriteLog(user2);

            //WriteEventLog($"user2: {user2}");

            IntPtr accountToken = WindowsIdentity.GetCurrent().Token;
            WindowsIdentity windowsIdentity = new WindowsIdentity(accountToken);
            var identity = windowsIdentity.Name;

            WriteLog(identity);


            //var user_name = System.DirectoryServices.AccountManagement.GroupPrincipal.FindByIdentity().Current.SamAccountName; //LogonUserIdentity.Name;
            //WriteLog(user_name);

            var Username = System.Security.Principal.WindowsIdentity.GetCurrent().Name;

            WriteLog(Username);

            //WindowsPrincipal wp = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            //var Username = wp.Identity.Name; // Username

            var name = Username.Split('\\');

            string UN = name[1].ToString();

            WriteLog(UN);

            var user = db.Ezy_Users.Where(x => x.UserID == UN).FirstOrDefault();

            if (user != null)
            {
                var UserRole = db.Ezy_Groupusers.Where(x => x.UserID == user.UserID).FirstOrDefault();

                string UserDefaultRole = UserRole == null ? string.Empty : UserRole.GroupName;

                //if (user.RoleID == 1 || UserDefaultRole == "Admins")
                //{
                //    string numbers = "0123456789";
                //    Random random = new Random();
                //    string otp = string.Empty;
                //    for (int i = 0; i < 5; i++)
                //    {
                //        int tempval = random.Next(0, numbers.Length);
                //        otp += tempval;
                //    }

                //    Session["msgotp"] = otp;
                //    Session["OTPLoginUser"] = user.UserID;
                //    string msg = "Your OTP from Moldtrax BD is " + otp;
                //    bool f = ShrdMaster.Instance.SendEmail(user.Email, "Subjected to OTP", msg);
                //    if (f)
                //    {
                //        TempData["ErrorMsg"] = "OTP has been sent to your email id. Please enter OTP.";
                //    }
                //    else
                //    {
                //        TempData["ErrorMsg"] = "Oops somethig went wrong. Please try again.";
                //    }

                //    return RedirectToAction("OTPLogin", "Account");
                //}
                //else
                //{
                   return ValidateUser(user);
               // }
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }


            //var TerminalInfo = GetTerminalServerClientNameWTSAPI();
        }

        public void WriteLog(string strLog)
        {
            try
            {
                string logFilePath = @"C:\Users\10333876\Documents\Log.txt";// Server.MapPath("~/logs/" + "Log.txt");
                //string logFilePath = Request.Url.Scheme + "://" + Request.Url.Authority + Url.Content("~/SiteImages/" + dbName + "/") + "Log.txt";
                FileInfo logFileInfo = new FileInfo(logFilePath);
                DirectoryInfo logDirInfo = new DirectoryInfo(logFileInfo.DirectoryName);
                if (!logDirInfo.Exists) logDirInfo.Create();
                using (FileStream fileStream = new FileStream(logFilePath, FileMode.Append))
                {
                    using (StreamWriter log = new StreamWriter(fileStream))
                    {
                        string NewLine = "======================== " + System.DateTime.Now + " =======================================================================";
                        log.WriteLine(NewLine);
                        log.WriteLine(strLog);
                        log.Flush();
                        log.Close();
                    }
                    fileStream.Close();
                }
            }
            catch (Exception ex)
            {

            }
        }

        public ActionResult ValidateUser(ezy_Users user)
        {
            FormsAuthentication.SetAuthCookie(user.UserID, true);
            Session["User"] = user.UserID;

            HttpCookie nameCookie = new HttpCookie("LoginMainUser");
            nameCookie.Values["LoginMainUser"] = user.UserID.ToString();
            Response.Cookies.Add(nameCookie);

            if (user.RoleID == 1)
            {
                var com = db.TblCompanies.ToList().OrderBy(x => x.CompanyName).FirstOrDefault();
                Session["CompanyID"] = com.CompanyID;
            }
            else
            {
                Session["CompanyID"] = user.CompanyID;
            }

            Session["RoleID"] = user.RoleID;

            SetupFormsAuthTicket(user, false);


            var dd = db.Ezy_Groupusers.Where(x => x.UserID == user.UserID).FirstOrDefault();


            if (dd != null)
            {
                if (dd.GroupName == "Admins")
                {
                    Session["Permission"] = 1;
                }
                else if (dd.GroupName == "Editors")
                {
                    Session["Permission"] = 2;
                }
                else if (dd.GroupName == "Repair Tech")
                {
                    Session["Permission"] = 3;
                }
                else if (dd.GroupName == "Process Tech")
                {
                    Session["Permission"] = 4;
                }
                else if (dd.GroupName == "Engineer")
                {
                    Session["Permission"] = 6;
                }
                else
                {
                    Session["Permission"] = 5;
                }
            }
            else
            {
                Session["Permission"] = 5;
            }

            LogTable LT = new LogTable();

            LT.Action = GetAction.Success.ToString();
            LT.PageName = GetTabName.User.ToString();
            LT.DataKey = 0;
            LT.TableName = GetDBTableName.ezy_Users.ToString();
            ShrdMaster.Instance.UpdateLog(LT);

            return RedirectToAction("Index", "DetailMoldInfo");
}


        public ActionResult ChangeLanguage(string lang)
        {
            new LanguageManager().SetLanguage(lang);
            return RedirectToAction("Index", "DetailMoldInfo");
        }

        public void UpdateActiveDirectoryRecord()
        {
            //DirectoryEntry startingPoint = new DirectoryEntry("LDAP://DC=blake,DC=equip");
            DirectoryEntry startingPoint = new DirectoryEntry("LDAP://DC=inx,DC=local");
            GETUsers(startingPoint);
        }

        public void GETUsers(DirectoryEntry startingPoint)
        {

            DirectoryEntry gbvision = startingPoint.Children.Find("CN=Users");

            DirectorySearcher searcher1 = new DirectorySearcher(gbvision);
            searcher1.Filter = "(objectCategory=User)";
            List<string> orgUnits = new List<string>();

            //orgUnits.Add("OU=DevOU,DC=infodatixhosting, DC=local");

            foreach (SearchResult res in searcher1.FindAll())
            {
                string pt = res.Path.ToString();
                string[] sds = pt.Split("//".ToCharArray());

                orgUnits.Add(sds[2]);
            }

            //string query = "truncate table Userdetails";

            //using (SqlConnection conn = new SqlConnection(ConString))
            //using (SqlCommand cmd = new SqlCommand(query, conn))
            //{
            //    conn.Open();
            //    cmd.ExecuteNonQuery();
            //    conn.Close();
            //}

            foreach (var x in orgUnits)
            {
                using (var context = new PrincipalContext(ContextType.Domain, "inx.local", x))
                {
                    using (var searcher = new PrincipalSearcher(new UserPrincipal(context)))
                    {
                        foreach (var result in searcher.FindAll())
                        {
                            ActiveDirectoryHelper af = new ActiveDirectoryHelper();
                            ADUserDetail ADDETAIL = af.GetUserByLoginName(result.SamAccountName);

                            //ADUserDetail FULLUSRDETAIL = af.GetUserDetailsByFullName(ADDETAIL.FirstName, ADDETAIL.MiddleName, ADDETAIL.LastName);

                            if (ADDETAIL != null)
                            {
                                string Name = "";
                                if (ADDETAIL.FirstName == "")
                                {
                                    Name = ADDETAIL.LoginName;
                                }

                                else
                                {
                                    Name = ADDETAIL.DisplayName;
                                }
                                string ADCon = "LDAP://inx.local/" + x;

                                //string ADCon = "LDAP://blake.equip/" + x;
                                ChangeVal(ADCon);
                            }

                        }
                    }
                }

            }

        }


        public void ChangeVal(string ADCon)
        {
            DirectoryEntry deUser = new DirectoryEntry(ADCon);
            //deUser.Username = "asp1";
            //deUser.Password = "Oscillate@123";

            ADUserDetail sd = new ADUserDetail(deUser);

            ezy_Users user = new ezy_Users();

            if (sd.EmailAddress != "")
            {
                user = db.Ezy_Users.Where(x => x.Email == sd.EmailAddress).FirstOrDefault();
            }


            if (user.UserID == null)
            {
               
                ezy_Users ud = new ezy_Users();
            ud.UserID = sd.LoginName;
            ud.FullName = (sd.FirstName + " " + sd.LastName) == " " ? sd.LoginName : (sd.FirstName + " " + sd.LastName);
            ud.OperatorStamp = null;
            ud.Password = ShrdMaster.Instance.ReturnUniqueName();
            ud.DateTimeStamp = System.DateTime.Now;
            ud.CompanyID = db.TblCompanies.Where(c=> c.CompanyName == sd.Company).Select(c=> c.CompanyID).FirstOrDefault();
            ud.RoleID = 1;
            ud.Email = sd.EmailAddress;
            ud.Mobile = sd.Mobile;
            ud.Manager = sd.ManagerName;
            ud.Department= sd.Department;
            ud.DisplayName = sd.DisplayName;

            db.Entry(ud).State = EntityState.Added;
            db.SaveChanges();
            db.Entry(ud).State = EntityState.Detached;

            }

        }


        private static string GetTerminalServerClientNameWTSAPI()
        {

            const int WTS_CURRENT_SERVER_HANDLE = -1;

            IntPtr buffer = IntPtr.Zero;
            uint bytesReturned;

            string strReturnValue = "";
            try
            {
                WTSQuerySessionInformation(IntPtr.Zero, WTS_CURRENT_SERVER_HANDLE, WTS_INFO_CLASS.WTSClientName, out buffer, out bytesReturned);
                strReturnValue = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(buffer);
            }

            finally
            {
                buffer = IntPtr.Zero;
            }

            return strReturnValue;
        }

        enum WTS_INFO_CLASS
        {
            WTSInitialProgram,
            WTSApplicationName,
            WTSWorkingDirectory,
            WTSOEMId,
            WTSSessionId,
            WTSUserName,
            WTSWinStationName,
            WTSDomainName,
            WTSConnectState,
            WTSClientBuildNumber,
            WTSClientName,
            WTSClientDirectory,
            WTSClientProductId,
            WTSClientHardwareId,
            WTSClientAddress,
            WTSClientDisplay,
            WTSClientProtocolType

        }

        [System.Runtime.InteropServices.DllImport("Wtsapi32.dll")]
        private static extern bool WTSQuerySessionInformation(System.IntPtr hServer, int sessionId, WTS_INFO_CLASS wtsInfoClass, out System.IntPtr ppBuffer, out uint pBytesReturned);

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            WriteLog("ttt");
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        public ActionResult Login1(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }


        public ActionResult OTPLogin()
        {
            return View();
        }

        [HttpPost]
        public ActionResult OTPLogin(string otp)
        {
            string Otp = string.Empty;
            if (!string.IsNullOrWhiteSpace(System.Web.HttpContext.Current.Session["msgotp"].ToString()))
            {
                Otp = System.Web.HttpContext.Current.Session["msgotp"].ToString();
            }

            if (otp == Otp)
            {
                string UserID = string.Empty;
                if (!string.IsNullOrWhiteSpace(System.Web.HttpContext.Current.Session["OTPLoginUser"].ToString()))
                {
                    UserID = System.Web.HttpContext.Current.Session["OTPLoginUser"].ToString();
                }

                var user = db.Ezy_Users.Where(x => x.UserID == UserID).FirstOrDefault();
                if (user != null)
                {
                   return ValidateUser(user);
                }

                return RedirectToAction("OTPLogin", "Account");
            }
            else
            {
                TempData["ErrorMsg"] = "OTP is not correct. Please enter correct OTP.";
                return RedirectToAction("OTPLogin", "Account");
            }

        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            Session.Abandon();
            Session.Clear();
            string[] myCookies = Request.Cookies.AllKeys;
            foreach (string cookie in myCookies)
            {
                Response.Cookies[cookie].Expires = DateTime.Now.AddDays(-1);
            }

            return RedirectToAction("Login", "Account");
        }


        public void CallUpdateLogFunc(LogTable obj)
        {
            try
            {
                if (obj != null)
                {
                    ShrdMaster.Instance.UpdateLog(obj);
                }
            }
            catch (Exception ex)
            {

            }
        }

        public void UpdateSession(int CID = 0)
        {
            Session["CompanyID"] = CID;
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(ezy_Users model, string returnUrl)
        {
            var user = System.Security.Principal.WindowsIdentity.GetCurrent().Name;

            LogTable LT = new LogTable();

            if (!ModelState.IsValid || model.Password == null)
            {
                TempData["ErrorMsg"] = "Invalid Login Attempt";
                return View(model);
            }

            //var sdfs = ShrdMaster.Instance.Decrypt("Moldtrax123");

            var EncryptPass = ShrdMaster.Instance.encrypt(model.Password);

            var result = db.Ezy_Users.Where(x => x.UserID == model.UserID && x.Password == EncryptPass).FirstOrDefault();

            if (result != null)
            {
               return ValidateUser(result);
            }

            LT.Action = GetAction.Unsuccess.ToString();
            LT.PageName = GetPageName.Login.ToString();
            LT.TableName = GetTabName.User.ToString();
            LT.DataKey = 0;

            Session["User"] = "";
            Session["CompanyID"] = "";
            ShrdMaster.Instance.UpdateLog(LT);
            TempData["ErrorMsg"] = "Invalid Login Attempt";
            return RedirectToAction("Login", "Account");
        }

        private ezy_Users SetupFormsAuthTicket(ezy_Users user, bool persistanceFlag)
        {
            var userId = user.UserID;
            var userData = userId.ToString(CultureInfo.InvariantCulture);
            var authTicket = new FormsAuthenticationTicket(1, //version
                                                        user.UserID, // user name
                                                        DateTime.Now,             //creation
                                                        DateTime.Now.AddMinutes(30), //Expiration
                                                        persistanceFlag, //Persistent
                                                        userData);
            var encTicket = FormsAuthentication.Encrypt(authTicket);
            Response.Cookies.Add(new HttpCookie(FormsAuthentication.FormsCookieName, encTicket));
            return user;
        }


        public ActionResult RedirectToLogin()
        {
            return PartialView("_RedirectToLogin");
        }

        [AllowAnonymous]
        public async Task<ActionResult> VerifyCode(string provider, string returnUrl, bool rememberMe)
        {
            if (!await SignInManager.HasBeenVerifiedAsync())
            {
                return View("Error");
            }
            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await SignInManager.TwoFactorSignInAsync(model.Provider, model.Code, isPersistent:  model.RememberMe, rememberBrowser: model.RememberBrowser);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(model.ReturnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid code.");
                    return View(model);
            }
        }

        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await SignInManager.SignInAsync(user, isPersistent:false, rememberBrowser:false);
                    
                    // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=320771
                    // Send an email with this link
                    // string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                    // var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                    // await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");

                    return RedirectToAction("Index", "Home");
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ConfirmEmail
        [AllowAnonymous]
        public async Task<ActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return View("Error");
            }
            var result = await UserManager.ConfirmEmailAsync(userId, code);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        //
        // GET: /Account/ForgotPassword
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindByNameAsync(model.Email);
                if (user == null || !(await UserManager.IsEmailConfirmedAsync(user.Id)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return View("ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=320771
                // Send an email with this link
                // string code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
                // var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);		
                // await UserManager.SendEmailAsync(user.Id, "Reset Password", "Please reset your password by clicking <a href=\"" + callbackUrl + "\">here</a>");
                // return RedirectToAction("ForgotPasswordConfirmation", "Account");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/ResetPassword
        [AllowAnonymous]
        public ActionResult ResetPassword(string code)
        {
            return code == null ? View("Error") : View();
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await UserManager.FindByNameAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            var result = await UserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            AddErrors(result);
            return View();
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        //
        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            // Request a redirect to the external login provider
            return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
        }

        //
        // GET: /Account/SendCode
        [AllowAnonymous]
        public async Task<ActionResult> SendCode(string returnUrl, bool rememberMe)
        {
            var userId = await SignInManager.GetVerifiedUserIdAsync();
            if (userId == null)
            {
                return View("Error");
            }
            var userFactors = await UserManager.GetValidTwoFactorProvidersAsync(userId);
            var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
            return View(new SendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/SendCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SendCode(SendCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            // Generate the token and send it
            if (!await SignInManager.SendTwoFactorCodeAsync(model.SelectedProvider))
            {
                return View("Error");
            }
            return RedirectToAction("VerifyCode", new { Provider = model.SelectedProvider, ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
        }

        //
        // GET: /Account/ExternalLoginCallback
        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                return RedirectToAction("Login");
            }

            // Sign in the user with this external login provider if the user already has a login
            var result = await SignInManager.ExternalSignInAsync(loginInfo, isPersistent: false);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = false });
                case SignInStatus.Failure:
                default:
                    // If the user does not have an account, then prompt the user to create an account
                    ViewBag.ReturnUrl = returnUrl;
                    ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
                    return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = loginInfo.Email });
            }
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Manage");
            }

            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await AuthenticationManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await UserManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await UserManager.AddLoginAsync(user.Id, info.Login);
                    if (result.Succeeded)
                    {
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                        return RedirectToLocal(returnUrl);
                    }
                }

                AddErrors(result);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        //
        // POST: /Account/LogOff
        [HttpPost]
        public ActionResult LogOff()
        {
            //AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            Session.Abandon();
            Session.Clear();
            string[] myCookies = Request.Cookies.AllKeys;
            foreach (string cookie in myCookies)
            {
                Response.Cookies[cookie].Expires = DateTime.Now.AddDays(-1);
            }
            //Session.Remove("LoginMainUser");
            return RedirectToAction("Login", "Account");
            //return RedirectToAction("Index", "Home");
        }

        //
        // GET: /Account/ExternalLoginFailure
        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }

                if (_signInManager != null)
                {
                    _signInManager.Dispose();
                    _signInManager = null;
                }
            }

            base.Dispose(disposing);
        }

        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        #endregion
    }
}