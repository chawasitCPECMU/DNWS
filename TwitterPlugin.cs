using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;

namespace DNWS
{
    class Following
    {
        public int FollowingId { get; set; }
        public string Name { get; set; }
    }
    class User
    {
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public string Session {get; set;}
        public List<Following> Following { get; set; } // Bug in SQLite implemention in EF7, no FK!
    }
    class Tweet
    {
        public int TweetId { get; set; }
        public string Message { get; set; }
        public string User { get; set; } // Bug in SQLite implemention in EF7, no FK!

        public DateTime DateCreated { get; set; }
    }
    // Ref: https://docs.microsoft.com/en-us/ef/core/get-started/netcore/new-db-sqlite
    class TweetContext : DbContext
    {
        public DbSet<Tweet> Tweets { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=tweet.db");
        }
    }

    class Twitter
    {
        User user;
        public Twitter(string name)
        {
            if (name == null || name == "")
            {
                throw new Exception("User name is required");
            }
            user = Twitter.GetUserFromName(name);
        }
        public string GetUsername()
        {
            return user.Name;
        }
        public String GetSession()
        {
            return user.Session;
        }
        public void RemoveFollowing(string followingName)
        {
            if (user == null)
            {
                throw new Exception("User is not set");
            }
            if (followingName == null)
            {
                throw new Exception("Following not found");
            }
            using (var context = new TweetContext())
            {
                Following following = new Following();
                following.Name = followingName;
                user.Following.Remove(following);
                context.Users.Update(user);
                context.SaveChanges();
            }
        }
        public void AddFollowing(string followingName)
        {
            if (user == null)
            {
                throw new Exception("User is not set");
            }
            if (followingName == null)
            {
                throw new Exception("Following not found");
            }
            using (var context = new TweetContext())
            {
                if (user.Following == null)
                {
                    user.Following = new List<Following>();
                }
                List<Following> followings = user.Following.Where(b => b.Name == followingName).ToList();
                if (followings.Count == 0) {
                    Following following = new Following();
                    following.Name = followingName;
                    user.Following.Add(following);
                    context.Users.Update(user);
                    context.SaveChanges();
                } 
            }
        }
        public List<Tweet> GetTimeline(User aUser)
        {
            if (aUser == null)
            {
                throw new Exception("User is not set");
            }
            List<Tweet> timeline;
            using (var context = new TweetContext())
            {
                timeline = context.Tweets.Where(b => b.User.Equals(aUser.Name)).OrderBy(b => b.DateCreated).ToList();
            }
            return timeline;
        }
        public List<Tweet> GetUserTimeline()
        {
            if (user == null)
            {
                throw new Exception("User is not set");
            }
            return GetTimeline(user);
        }

        public List<Tweet> GetFollowingTimeline()
        {
            if (user == null)
            {
                throw new Exception("User is not set");
            }
            List<Tweet> timeline = new List<Tweet>();
            using (var context = new TweetContext())
            {
                List<Following> followings = user.Following;
                if (followings == null || followings.Count == 0)
                {
                    return null;
                }
                foreach (Following following in followings)
                {
                    User followingUser = Twitter.GetUserFromName(following.Name);
                    timeline.AddRange(GetTimeline(followingUser));
                }
                timeline.AddRange(GetTimeline(user));
            }
            timeline = timeline.OrderBy(b => b.DateCreated).ToList();
            return timeline;
        }
        public void PostTweet(string message)
        {
            if (user == null)
            {
                throw new Exception("User is not set");
            }
            Tweet tweet = new Tweet();
            tweet.User = user.Name;
            tweet.Message = message;
            tweet.DateCreated = DateTime.Now;
            using (var context = new TweetContext())
            {
                context.Tweets.Add(tweet);
                context.SaveChanges();
            }
        }

        public static void AddUser(string name, string password)
        {
            User user = new User();
            user.Name = name;
            user.Password = password;
            using (var context = new TweetContext())
            {
                List<User> userlist = context.Users.Where(b => b.Name.Equals(name)).ToList();
                if (userlist.Count > 0)
                {
                    throw new Exception("User already exists");
                }
                context.Users.Add(user);
                context.SaveChanges();
            }
        }

        public static bool IsValidUser(string name, string password)
        {
            using (var context = new TweetContext())
            {
                List<User> userlist = context.Users.Where(b => b.Name.Equals(name) && b.Password.Equals(password)).ToList();
                if (userlist.Count == 1)
                {
                    return true;
                }
            }
            return false;
        }
        public static User GetUserFromSession(string session)
        {
            using (var context = new TweetContext())
            {
                try
                {
                    List<User> users = context.Users.Where(b => b.Session.Equals(session)).Include(b => b.Following).ToList();
                    return users[0];
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public static void RemoveSession(string username)
        {
            using (var context = new TweetContext())
            {
                List<User> users = context.Users.Where(b => b.Name.Equals(username)).ToList();
                User aUser = users[0];
                aUser.Session = null;
                context.Users.Update(aUser);
                context.SaveChanges();
            }
        }
        public static string GenSession(string username)
        {
            try
            {
                using (MD5 md5 = MD5.Create())
                {
                    md5.Initialize();
                    md5.ComputeHash(Encoding.UTF8.GetBytes(username + DateTime.Now.ToString()));
                    // It's annoying that toString is not working here.
                    StringBuilder sbhash = new StringBuilder();
                    byte[] hash = md5.Hash;
                    for (int i = 0; i < hash.Length; i++)
                    {
                        sbhash.Append(hash[i].ToString("x2"));
                    }
                    string newSession = sbhash.ToString();
                    //Update session.
                    using (var context = new TweetContext())
                    {
                        List<User> users = context.Users.Where(b => b.Name.Equals(username)).ToList();
                        User aUser = users[0];
                        aUser.Session = newSession;
                        context.Users.Update(aUser);
                        context.SaveChanges();
                    }
                    return newSession;
                }

            }
            catch (Exception)
            {
                return null;
            }

        }
        public static User GetUserFromName(string name)
        {
            using (var context = new TweetContext())
            {
                try
                {
                    List<User> users = context.Users.Where(b => b.Name.Equals(name)).Include(b => b.Following).ToList();
                    return users[0];
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }
    }
    public class TwitterPlugin : IPlugin
    {
        public HTTPResponse PostProcessing(HTTPResponse response)
        {
            throw new NotImplementedException();
        }

        public void PreProcessing(HTTPRequest request)
        {
            throw new NotImplementedException();
        }

        private StringBuilder GenTimeline(Twitter twitter, StringBuilder sb)
        {
            sb.Append("Say something<br />");
            sb.Append("<form method=\"post\">");
            sb.Append("<input type=\"text\" name=\"message\"></input>");
            sb.Append("<input type=\"hidden\" name=\"session\" value=\"").Append(twitter.GetSession()).Append("\" />");
            sb.Append("<input type=\"submit\" name=\"action\" value=\"tweet\" /> <br />");
            sb.Append("</form>");
            sb.Append("Follow someone<br />");
            sb.Append("<form method=\"post\">");
            sb.Append("<input type=\"text\" name=\"following\"></input>");
            sb.Append("<input type=\"hidden\" name=\"session\" value=\"").Append(twitter.GetSession()).Append("\" />");
            sb.Append("<input type=\"submit\" name=\"action\" value=\"following\" /> <br />");
            sb.Append("</form>");
            sb.Append(String.Format("<h3><b>{0}</b>'s timeline</h3><br />", twitter.GetUsername()));
            List<Tweet> tweets = twitter.GetUserTimeline();
            foreach (Tweet tweet in tweets)
            {
                sb.Append("[" + tweet.DateCreated + "]" + tweet.User + ":" + tweet.Message + "<br />");
            }
            sb.Append("<br /><br />");
            sb.Append("<h3>Following timeline</h3><br />");
            tweets = twitter.GetFollowingTimeline();
            if (tweets == null)
            {
                sb.Append("Your following list is empty, follow someone!");
            }
            else
            {
                foreach (Tweet tweet in tweets)
                {
                    sb.Append("[" + tweet.DateCreated + "] " + tweet.User + ":" + tweet.Message + "<br />");
                }
            }
            return sb;
        }

        protected StringBuilder GenLoginPage(StringBuilder sb)
        {
            sb.Append("<h2>Login</h2>");
            sb.Append("<form method=\"get\">");
            sb.Append("Username: <input type=\"text\" name=\"username\" value=\"\" /> <br />");
            sb.Append("Password: <input type=\"password\" name=\"password\" value=\"\" /> <br />");
            sb.Append("<input type=\"submit\" name=\"action\" value=\"login\" /> <br />");
            sb.Append("</form>");
            sb.Append("<br /><br /><br />");
            sb.Append("<h2>New user</h2>");
            sb.Append("<form method=\"get\">");
            sb.Append("Username: <input type=\"text\" name=\"username\" value=\"\" /> <br />");
            sb.Append("Password: <input type=\"password\" name=\"password\" value=\"\" /> <br />");
            sb.Append("<input type=\"submit\" name=\"action\" value=\"newuser\" /> <br />");
            sb.Append("</form>");
            return sb;
        }


        public HTTPResponse GetResponse(HTTPRequest request)
        {
            HTTPResponse response = new HTTPResponse(200);
            StringBuilder sb = new StringBuilder();
            string session = request.GetRequestByKey("session");
            string action = request.GetRequestByKey("action").ToLower();
            if(action != null) action = action.ToLower();
            string username = request.GetRequestByKey("username");
            string password = request.GetRequestByKey("password");
            string following = request.GetRequestByKey("following");
            string message = request.GetRequestByKey("message");
            if (session == null) // no session? show login screen
            {
                if (action == null) {
                    sb.Append("<h1>Twitter</h1>");
                    sb = GenLoginPage(sb);
                }
                else
                {
                    if (action.Equals("newuser"))
                    {
                        if (username != null && password != null && username != "" && password != "")
                        {
                            try
                            {
                                Twitter.AddUser(username, password);
                                sb.Append("User added successfully, please go back to <a href=\"/twitter\">login page</a> to login");
                            }
                            catch (Exception ex)
                            {
                                sb.Append(String.Format("Error adding user with error [{0}], please go back to <a href=\"/twitter\">login page</a> to try again", ex.Message));
                            }
                        }
                    }
                    else if (action.Equals("login"))
                    {
                        if (username != null && password != null && username != "" && password != "")
                        {
                            if (Twitter.IsValidUser(username, password))
                            {
                                string newSession = Twitter.GenSession(username);
                                if(newSession != null) {
                                    response.Status = 301;
                                    response.AddCustomHeader("Location", "/twitter?session=" + newSession);
                                    return response;
                                }
                                response.Status = 500;
                                return response;
                            }
                            else
                            {
                                sb.Append("Error login, please go back to <a href=\"/twitter\">login page</a> to try again");
                            }
                        }
                    }
                }
            }
            else // session is not null
            {
                User user = Twitter.GetUserFromSession(session);
                if(user == null) {
                    response.Status = 404;
                    return response;
                }
                Twitter twitter = new Twitter(user.Name);
                if (action == null) // No action? go to homepage
                {
                    try
                    {
                        sb.Append(String.Format("<h1>{0}'s Twitter</h1>", user.Name));
                        sb = GenTimeline(twitter, sb);
                    }
                    catch (Exception ex)
                    {
                        sb.Append(String.Format("Error [{0}], please go back to <a href=\"/twitter\">login page</a> to try again", ex.Message));
                    }
                } else { //action is not null
                    sb.Append(String.Format("<h1>{0}'s Twitter</h1>", user.Name));
                    if (action.Equals("following"))
                    {
                        try
                        {
                            twitter.AddFollowing(following);
                            sb = GenTimeline(twitter, sb);
                        }
                        catch (Exception ex)
                        {
                            sb.Append(String.Format("Error [{0}], please go back to <a href=\"/twitter\">login page</a> to try again", ex.Message));
                        }
                    }
                    else if (action.Equals("tweet"))
                    {
                        try
                        {
                            twitter.PostTweet(message);
                            sb = GenTimeline(twitter, sb);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                            sb.Append(String.Format("Error [{0}], please go back to <a href=\"/twitter\">login page</a> to try again", ex.Message));
                        }
                    }
                }
            }
            response.Body = Encoding.UTF8.GetBytes(sb.ToString());
            return response;
        }
    }
}