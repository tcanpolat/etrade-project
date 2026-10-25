using ETICARET.Business.Abstract;
using ETICARET.WebUI.EmailService;
using ETICARET.WebUI.Extensions;
using ETICARET.WebUI.Identity;
using ETICARET.WebUI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ETICARET.WebUI.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ICartService _cartService;

        public AccountController(UserManager<ApplicationUser> userManager,SignInManager<ApplicationUser> signInManager,ICartService cartService)
        {
            _cartService = cartService;
            _signInManager = signInManager;
            _userManager = userManager;
        }
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new ApplicationUser()
            {
                FullName = model.FullName,
                UserName = model.UserName,
                Email = model.Email
            };

            var result = await _userManager.CreateAsync(user,model.Password);

            if (result.Succeeded)
            {
                // Email confirmation token oluşturma
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                var callbackUrl = Url.Action("ConfirmEmail", "Account",
                    new
                    {
                        userId = user.Id,
                        token = code
                    }
                );

                string siteUrl = "https://localhost:7120";
                string activeUrl = $"{siteUrl}{callbackUrl}"; // https://localhost:7120/Account/ConfirmEmail?userId=1&token=fmafjkalkfljkajfkla

                string body = $"Hesabınızı aktifleştirmek için <a href='{activeUrl}'>tıklayınız</a>";

                // Email servisi ile email gönderme işlemi yapılacak.
                var sonuc = MailHelper.SendEmail(body, user.Email, "Hesabınızı Aktifleştirin");

                if (sonuc)
                {
                    TempData.Put("message", new ResultModel()
                    {
                        Title ="Hesap Aktifleştirme",
                        Message = "Mail Adresinize aktifleştirme linki gönderilmiştir. Hesabınızı onaylamak için linke tıklayınız.",
                        Css = "success"
                    });
                }

                return RedirectToAction("Login","Account");
            }
            
            TempData.Put("message", new ResultModel()
            {
                Title ="Kayıt Hatası",
                Message = "Kullanıcı kaydınız oluşturulurken bir hata oluştu!",
                Css = "danger"
            });
            
            return View(model);
        }

        public async Task<IActionResult> ConfirmEmail(string userId,string token)
        {
            if(userId == null || token == null)
            {
                TempData.Put("message", new ResultModel()
                {
                    Title ="Geçersiz Email",
                    Message = "Hesabınız aktifleştirilemedi!",
                    Css = "danger"
                });

                return RedirectToAction("Index", "Home");
            }

            var user = await _userManager.FindByIdAsync(userId);

            if(user != null)
            {
                var result = await _userManager.ConfirmEmailAsync(user, token); // EmailComfirmedı 1 yap

                if (result.Succeeded)
                {
                    // kullanıcının maili başarıyla confir edildiyse sepeti oluşturulacak
                    _cartService.InitialCart(user.Id);
                    TempData.Put("message", new ResultModel()
                    {
                        Title ="Hesabınız aktifleştirme.",
                        Message = "Hesabınız başarıyla aktifleştirildi",
                        Css = "success"
                    });
                    return RedirectToAction("Login", "Account");
                }
            
            }

            TempData.Put("message", new ResultModel()
            {
                Title ="Hesabınız aktifleştirme hatası",
                Message = "Hesabınız aktifleştirilirken bir hata oluştu.Lütfen tekrar deneyiniz",
                Css = "danger"
            });

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Login(string returnUrl = null)
        {
            return View(
                new LoginModel()
                {
                    ReturnUrl = returnUrl
                }    
            );
        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model)
        {
            ModelState.Remove("ReturnUrl");

            if (!ModelState.IsValid)
            {
                TempData.Put("message", new ResultModel()
                {
                    Title ="Giriş Hatası",
                    Message = "Lütfen giriş bilgilerinizi kontrol ediniz.",
                    Css = "danger"
                });

                return View(model);
            }

           var user = await _userManager.FindByEmailAsync(model.Email);

            if(user is null)
            {
                ModelState.AddModelError("", "Bu email ile bir kullanıcı bulanamadı.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(user,model.Password,true,true);

            if (result.Succeeded)
            {
                return Redirect(model.ReturnUrl ?? "~/");
            }
            if (result.IsLockedOut)
            {
                TempData.Put("message", new ResultModel()
                {
                    Title ="Hesabınız kilitlendi",
                    Message = "Hesabınız geçiçi olarak kilitlenmiştir. Lütfen 1 dakika sonra tekrar deneyiniz",
                    Css = "danger"
                });

                return View(model);
            }


            if (result.IsNotAllowed)
            {
                TempData.Put("message", new ResultModel()
                {
                    Title ="Hesabınız onaylanmadı",
                    Message = "Lütfen mailinize gelen onaylama linkine tıklayarak hesabınızı onaylayın",
                    Css = "danger"
                });

                return View(model);
            }


            ModelState.AddModelError("", "Email ya da Şifreniz hatalıdır");

            return View(model);


        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            TempData.Put("message", new ResultModel()
            {
                Title ="Oturumunuz Kapatıldı",
                Message = "Hesabınızdan başarıyla çıkış yaptınız.",
                Css = "success"
            });

            return RedirectToAction("Index","Home");
        }

        public IActionResult ForgotPassword()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                TempData.Put("message", new ResultModel()
                {
                    Title ="Hata",
                    Message = "Lütfen email adresinizi giriniz.",
                    Css = "danger"
                });

                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);

            if(user is null)
            {
                TempData.Put("message", new ResultModel()
                {
                    Title ="Hata",
                    Message = "Bu mail adresi ile bir kullanıcı bulunamadı.",
                    Css = "danger"
                });

                return View();
            }

            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Action("ResetPassword", "Account",
                   new
                   {
                       token = code
                   }
               );

            string siteUrl = "https://localhost:7120";
            string activeUrl = $"{siteUrl}{callbackUrl}";

            string body = $"Parolanızı yenilemek için <a href='{activeUrl}'>tıklayınız</a>";

            bool sonuc = MailHelper.SendEmail(body, email, "ETİCARET Parola Sıfırlama");

            TempData.Put("message", new ResultModel()
            {
                Title ="Parola Sıfırlama",
                Message = "Parola sıfırlama linki mailinize gönderilmiştir.",
                Css = "success"
            });

            return RedirectToAction("Login","Account");

        }

        public IActionResult ResetPassword(string token)
        {
            if(token == null)
            {
                TempData.Put("message", new ResultModel()
                {
                    Title ="Parola Sıfırlama",
                    Message = "Parolanız sıfırlanırken bir hata oluştu.",
                    Css = "danger"
                });
                return RedirectToAction("Index", "Home");
            }

            return View(
                new ResetPasswordModel()
                {
                    Token = token
                }    
            );
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);

            if(user == null)
            {
                TempData.Put("message", new ResultModel()
                {
                    Title ="Parola Yenileme Hatası",
                    Message = "Bu mail adresi ile bir kullanıcı bulunamadı.",
                    Css = "danger"
                });

                return RedirectToAction("Index", "Home");
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);

            if (result.Succeeded)
            {
                TempData.Put("message", new ResultModel()
                {
                    Title ="Parola Yenileme",
                    Message = "Parolanız başarıyla yenilenmiştir.",
                    Css = "success"
                });

                return RedirectToAction("Login", "Account");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);

        }

        public async Task<IActionResult> Manage()
        {
            var user = await _userManager.GetUserAsync(User);

            if(user == null)
            {
                TempData.Put("message", new ResultModel()
                {
                    Title ="Hata",
                    Message = "Kullanıcı bilgilerine ulaşılamadı.",
                    Css = "danger"
                });

                return View();
            }

            var model = new AccountModel()
            {
                FullName = user.FullName,
                Email = user.Email,
                UserName = user.UserName,
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Manage(AccountModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData.Put("message", new ResultModel()
                {
                    Title ="Hata",
                    Message = "Lütfen bilgilerinizi kontrol ediniz.",
                    Css = "danger"
                });
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);

            if(user == null)
            {
                TempData.Put("message", new ResultModel()
                {
                    Title ="Hata",
                    Message = "Kullanıcı bilgilerinize ulaşılamadı.",
                    Css = "danger"
                });

                return View(model);
            }

            // Güncelleme işlemleri
            user.FullName = model.FullName;
            user.UserName = model.UserName;
            user.Email = model.Email;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData.Put("message", new ResultModel()
                {
                    Title ="Profile Güncelleme",
                    Message = "Profiliniz başarıyla güncellenmiştir.",
                    Css = "success"
                });

                return RedirectToAction("Manage");
            }

            TempData.Put("message", new ResultModel()
            {
                Title ="Profile Güncelleme Hatası",
                Message = "Profiliniz güncellemeniz başarısız olmuştur.",
                Css = "danger"
            });

            return View(model);
        }
    }
}
