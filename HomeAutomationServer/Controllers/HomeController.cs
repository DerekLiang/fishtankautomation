using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using HomeAutomationServer.Models;
using log4net;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace HomeAutomationServer.Controllers
{
    public class HomeController : Controller
    {
        private static log4net.ILog log = LogManager.GetLogger(typeof(HomeController));

     
        public async Task<ActionResult> Index()
        {
            log.Debug("home page rendered..");
            var biz = new Biz();
            var currentApplicationUser = biz.getAppUser(User);

            ViewBag.APIKEY = currentApplicationUser == null ? "" : currentApplicationUser.ApiKey.ToString("n");

            return View();
        }

        public ActionResult SchematicDiagram()
        {
            log.Debug("SchematicDiagram rendered..");
            return View();
        }

        public ActionResult SoftwareArchitecture()
        {
            log.Debug("SchematicDiagram rendered..");

            return View();
        }
        public ActionResult PhotoGallery()
        {
            log.Debug("PhotoGallery rendered..");

            return View();
        }        
        
        public ActionResult RaspberryPi()
        {
            log.Debug("RaspberryPi rendered..");

            return View();
        }
    }
}