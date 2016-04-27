﻿using ClashOfTheCharacters.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet;
using Microsoft.AspNet.Identity;
using ClashOfTheCharacters.ViewModels;
using ClashOfTheCharacters.Services;

namespace ClashOfTheCharacters.Controllers
{
    public class UserController : Controller
    {
        ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            var userId = User.Identity.GetUserId();
            var user = db.Users.Find(userId);

            var userViewModel = new UserViewModel
            {
                Username = user.UserName,
                WonBattles = db.Competitors.Where(c => c.UserId == userId && c.Winner).Count(),
                LostBattles = db.Competitors.Where(c => c.UserId == userId && !c.Winner).Count(),
                MostUsedCharacter = user.TeamMembers.OrderByDescending(t => t.BattleAppearances.Count()).First(),
                MostValuedCharacter = user.TeamMembers.OrderByDescending(t => t.Kills).First(),
                TotalGoldEarned = db.Competitors.Any(c => c.UserId == userId) ? db.Competitors.Where(c => c.UserId == userId).Sum(c => c.GoldEarned) : 0
            };

            return View(userViewModel);
        }

        [ChildActionOnly]
        public ActionResult ProfilePartial()
        {
            RunServices();

            var db = new ApplicationDbContext();

            var userId = User.Identity.GetUserId();
            var user = db.Users.Find(userId);

            int index = 0;

            foreach (var u in db.Users.OrderByDescending(u => u.LadderPoints))
            {
                index++;

                if (u.Id == userId)
                {
                    break;
                }
            }

            var profilePartialViewModel = new ProfilePartialViewModel
            {
                UserId = userId,
                Username = user.UserName,
                ImageUrl = user.ImageUrl == null ? "/Images/Other/noprofilepicture.jpg" : user.ImageUrl,
                Rank = index,
                Gold = user.Gold,
                LadderPoints = user.LadderPoints,
                Stamina = user.Stamina,
                MaxStamina = user.MaxStamina,
                Xp = user.Xp,
                MaxXp = user.MaxXp,
                Level = user.Level,
                NextStaminaMinutes = 10 - (DateTimeOffset.Now - user.LastStaminaTime).Minutes,
                TeamMembers = user.TeamMembers.OrderBy(tm => tm.Slot).ToList(),
                RecentBattles = db.Battles.Where(b => (b.Challenge.ChallengerId == userId || b.Challenge.ReceiverId == userId) && b.Calculated).OrderByDescending(b => b.StartTime).Take(10).ToList(),
                OngoingBattles = db.Battles.Where(b => (b.Challenge.ChallengerId == userId || b.Challenge.ReceiverId == userId) && !b.Calculated).ToList()
            };

            return PartialView("_ProfilePartial", profilePartialViewModel);
        }

        [HttpPost]
        public ActionResult UpdateProfilePicture()
        {
            var userId = User.Identity.GetUserId();
            var user = db.Users.Find(userId);
            var imageUrl = Request.Form.Get("imageUrl");

            user.ImageUrl = imageUrl;
            db.SaveChanges();

            return Redirect(Request.UrlReferrer.PathAndQuery);
        }

        void RunServices()
        {
            var db = new ApplicationDbContext();

            var userId = User.Identity.GetUserId();
            var user = db.Users.Find(userId);

            user.LastActive = DateTimeOffset.Now;
            db.SaveChanges();

            if (user.Stamina < user.MaxStamina)
            {
                var staminaService = new StaminaService();
                staminaService.UpdateStamina(userId);
            }

            var battleService = new BattleService();
            battleService.RunBattles();
        }
    }
}