using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DNWS
{
    public class TwitterAPIPlugin : TwitterPlugin, IPlugin
    {
        private const string USER_ACTION = "user";
        private const string TWEET_ACTION = "tweet";
        private const string LOGIN_ACTION = "login";
        private const string LOGOUT_ACTION = "logout";
        private const string FOLLOWING_ACTION = "following";
        private string session;
        private string action;
        private string requestMethod;
        private const string HTTP_GET = HTTPRequest.METHOD_GET;
        private const string HTTP_POST = HTTPRequest.METHOD_POST;
        private const string HTTP_DELETE = HTTPRequest.METHOD_DELETE;
        private const string HTTP_OPTIONS = HTTPRequest.METHOD_OPTIONS;

        protected bool IsAction(string _action)
        {
            return _action.ToLower().Equals(action.ToLower());
        }
        protected bool IsMethod(string _method)
        {
            return requestMethod.ToLower().Equals(_method.ToLower());
        }
        protected Func<string, bool> IsNOE = String.IsNullOrEmpty;
        public new HTTPResponse GetResponse(HTTPRequest request)
        {
            HTTPResponse response = new HTTPResponse(200);
            StringBuilder sb = new StringBuilder();
            session = request.GetPropertyByKey("X-session");
            string[] urlToken = request.Url.Split("/");
            if (urlToken.Length > 2)
            {
                action = urlToken[2];
            }
            else
            {
                action = null;
            }
            requestMethod = request.Method;
            string username = request.GetRequestByKey("username");
            string password = request.GetRequestByKey("password");
            if(IsMethod(HTTP_OPTIONS)) {
                return response;
            }

            if (IsAction(USER_ACTION))
            {
                if (IsMethod(HTTP_POST))
                {
                    if (!IsNOE(username) && !IsNOE(password))
                    {
                        try
                        {
                            Twitter.AddUser(username, password);
                            response.Status = 201;
                        }
                        catch (Exception ex)
                        {
                            response.SetBody(ex.Message);
                            response.Status = 500;
                        }
                    }
                    else
                    {
                        response.SetBody("Username and password required");
                        response.Status = 400;
                        return response;
                    }
                }
            }
            else if (IsAction(LOGIN_ACTION))
            {
                if (IsMethod(HTTP_POST))
                {
                    if (!IsNOE(username) && !IsNOE(password))
                    {
                        if (Twitter.IsValidUser(username, password))
                        {
                            string newSession = Twitter.GenSession(username);
                            if (!IsNOE(newSession))
                            {
                                response.Status = 201;
                                dynamic jobj = new JObject();
                                jobj.Session = newSession;
                                response.Type = "application/json";
                                string resp = JsonConvert.SerializeObject(jobj);
                                response.Body = Encoding.UTF8.GetBytes(resp);
                                return response;
                            }
                            response.SetBody("Can't create session");
                            response.Status = 500;
                            return response;
                        }
                        else
                        {
                            response.SetBody("Invalid username/password");
                            response.Status = 404;
                            return response;
                        }
                    }
                    else
                    {
                        response.SetBody("Username and password required");
                        response.Status = 400;
                        return response;
                    }

                }
            }
            else if (!IsNOE(session))
            {
                User user = Twitter.GetUserFromSession(session);
                if (user == null)
                {
                    response.SetBody("User not found, please login again");
                    response.Status = 404;
                    return response;
                }
                Twitter twitter = new Twitter(user.Name);
                if (IsNOE(action))
                {
                    if (IsMethod(HTTP_GET))
                    {
                        try
                        {
                            response = new HTTPResponse(200);
                            response.Type = "application/json";
                            string resp = JsonConvert.SerializeObject(twitter.GetFollowingTimeline());
                            if (IsNOE(resp))
                            {
                                response.SetBody("No post in timline");
                                response.Status = 404;
                                return response;
                            }
                            response.Body = Encoding.UTF8.GetBytes(resp);
                            return response;
                        }
                        catch (Exception ex)
                        {
                            response.SetBody(ex.Message);
                            response.Status = 500;
                            return response;
                        }
                    }
                }
                else if (IsAction(LOGOUT_ACTION)) 
                {
                    if (IsMethod(HTTP_POST))
                    {
                        try {
                            Twitter.RemoveSession(user.Name);
                            response.Status = 200;
                            return response;
                        }
                        catch (Exception ex)
                        {
                            response.SetBody(ex.Message);
                            response.Status = 500;
                            return response;
                        }
                    }
                }
                else if (IsAction(TWEET_ACTION))
                {
                    if (IsMethod(HTTP_GET))
                    {
                        try
                        {
                            response = new HTTPResponse(200);
                            response.Type = "application/json";
                            string resp = null;
                            if(IsNOE(urlToken[3])) {
                                resp = JsonConvert.SerializeObject(twitter.GetTimeline(user));
                            }
                            else
                            {
                                User aUser = Twitter.GetUserFromName(urlToken[3]);
                                if(aUser == null) {
                                    response.SetBody("User not found");
                                    response.Status = 404;
                                    return response;
                                }
                                resp = JsonConvert.SerializeObject(twitter.GetTimeline(aUser));
                            }
                            if (IsNOE(resp))
                            {
                                response.SetBody("No post in timline");
                                response.Status = 404;
                                return response;
                            }
                            response.Body = Encoding.UTF8.GetBytes(resp);
                            return response;
                        }
                        catch (Exception ex)
                        {
                            response.SetBody(ex.Message);
                            response.Status = 500;
                            return response;
                        }
                    }
                    else if (IsMethod(HTTP_POST))
                    {
                        try
                        {
                            string message = request.GetRequestByKey("message");
                            if (IsNOE(message)) {
                                response.SetBody("Message required");
                                response.Status = 400;
                                return response;
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
                            response.Status = 201;
                            return response;
                        }
                        catch (Exception ex)
                        {
                            response.SetBody(ex.Message);
                            response.Status = 500;
                            return response;
                        }

                    }
                    else if (IsMethod(HTTP_DELETE))
                    {
                        try {
                            throw (new NotImplementedException());
                        } catch (Exception ex) {
                            response.SetBody(ex.Message);
                            response.Status = 501;
                            return response;
                        }
                    }
                }
                else if (IsAction(FOLLOWING_ACTION))
                {
                    if (IsMethod(HTTP_GET))
                    {
                        response = new HTTPResponse(200);
                        response.Type = "application/json";
                        string resp = JsonConvert.SerializeObject(user.Following);
                        if (resp == null)
                        {
                            response.SetBody("No following");
                            response.Status = 404;
                            return response;
                        }
                        response.Body = Encoding.UTF8.GetBytes(resp);
                        return response;
                    }
                    else if (IsMethod(HTTP_POST))
                    {
                        string following = request.GetRequestByKey("followingname");
                        try
                        {
                            twitter.AddFollowing(following);
                            response = new HTTPResponse(201);
                            response.Type = "application/json";
                            string resp = JsonConvert.SerializeObject(user.Following);
                            if (resp == null)
                            {
                                response.SetBody("No folowing");
                                response.Status = 404;
                                return response;
                            }
                            response.Body = Encoding.UTF8.GetBytes(resp);
                            return response;
                        }
                        catch (Exception ex)
                        {
                            response.Status = 400;
                            response.SetBody(ex.Message);
                            return response;

                        }
                    }
                    else if (IsMethod(HTTP_DELETE))
                    {
                        string following = request.GetRequestByKey("followingname");
                        try
                        {
                            twitter.RemoveFollowing(following);
                            response = new HTTPResponse(200);
                            response.Type = "application/json";
                            string resp = JsonConvert.SerializeObject(user.Following);
                            if (resp == null)
                            {
                                response.SetBody("No following");
                                response.Status = 404;
                                return response;
                            }
                            response.Body = Encoding.UTF8.GetBytes(resp);
                            return response;
                        }
                        catch (Exception ex)
                        {
                            response.Status = 400;
                            response.SetBody(ex.Message);
                            return response;

                        }
                    }
                }
            }
            response.Status = 400;
            return response;
        }
    }
}