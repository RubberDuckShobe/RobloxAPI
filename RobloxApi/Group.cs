﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RobloxApi
{
    public class Group
    {

        /// <summary>
        /// The ID of the group.
        /// </summary>
        [JsonProperty("Id")]
        public int ID
        {
            get;
            private set;
        }

        /// <summary>
        /// Roles in a group as an array.
        /// </summary>
        [JsonProperty("Roles")]
        public GroupRole[] Roles
        {
            get;
            private set;
        }
        
        /// <summary>
        /// The name of the group as shown on the website.
        /// </summary>
        [JsonProperty("Name")]
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// The owner of the group as a User, will be null if no one owns the group.
        /// </summary>
        [JsonProperty("Owner")]
        public User Owner
        {
            get;
            private set;
        }
        
        /// <summary>
        /// The icon of the group.
        /// </summary>
        [JsonProperty("EmblemUrl")]
        public string EmblemUrl
        {
            get;
            private set;
        }

        /// <summary>
        /// The description of the group as shown on the website. Can be empty.
        /// </summary>
        [JsonProperty("Description")]
        public string Description
        {
            get;
            private set;
        }

        private Group()
        {

        }

        /// <summary>
        /// Gets the clan of this group if any, will be null if no clan exists for this group.
        /// </summary>
        /// <returns>Gets the clan of this group if any, will be null if no clan exists for this group.</returns>
        public async Task<Clan> ToClan()
        {
            return await Clan.FromID(ID);
        }

        public Group(int groupId)
        {
            ID = groupId;
        }

        public override string ToString()
        {
            return string.Format("RobloxGroup ({0}): ID: {1} Name: {2}", GetHashCode(), ID, Name);
        }

        /// <summary>
        /// Checks if a user is in the group. If an http exeption occurs then it will return false.
        /// </summary>
        /// <param name="user">The user to check.</param>
        /// <returns>A boolean dictating if the user is in this group.</returns>
        public async Task<bool> IsUserInGroup(User user)
        {
            string stringifiedJSON = await HttpHelper.GetStringFromURL(string.Format("https://groups.roblox.com/v1/users/{0}/groups/roles", user.ID));
            JObject obj = JObject.Parse(stringifiedJSON);
            JToken dataToken;

            if (obj.TryGetValue("data", out dataToken))
            {
                JArray arr = (JArray)dataToken;
                foreach(JObject groupMembershipObject in arr)
                {
                    JToken groupObject;
                    if(groupMembershipObject.TryGetValue("group", out groupObject))
                    {
                        JToken idToken;
                        if(((JObject)groupObject).TryGetValue("id", out idToken))
                        {
                            if(idToken.Value<int>() == ID)
                            {
                                return true;
                            }
                        }
                        else
                        {
                            throw new Exception("Token \"group.id\" could not be found in groupMembershipObject.");
                        }
                    }
                    else
                    {
                        throw new Exception("Token \"group\" could not be found in groupMembershipObject.");
                    }
                }
                return false;
            }
            else
            {
                throw new Exception("Could not find \"data\" in json response. Did the response model change?");
            }
        }

        /// <summary>
        /// Gets a group object using the groupId.
        /// </summary>
        /// <param name="groupId">Group to get</param>
        /// <returns>The group object</returns>
        public static async Task<Group> FromID(int groupId)
        {
            try
            {
                Group group = new Group();
                group.ID = groupId;

                string data = await HttpHelper.GetStringFromURL(string.Format("https://api.roblox.com/groups/{0}", groupId));

                JObject obj = JObject.Parse(data);

                group.Name = (string)obj["Name"];

                JObject jOwner = (JObject)obj["Owner"];

                if (jOwner != null)
                {
                    User owner = new User((int)jOwner["Id"]);
                    owner.Username = (string)jOwner["Name"];

                    group.Owner = owner;
                }

                group.EmblemUrl = (string)obj["EmblemUrl"];
                group.Description = (string)obj["Description"];

                /*
                JArray roles = obj.Value<JArray>("Roles");

                group.Roles = new GroupRole[roles.Count];

                for (int i = 0; i < group.Roles.Length; i++)
                {
                    JObject o = (JObject)roles[i];
                    group.Roles[i] = new GroupRole((string)o["Name"], (int)o["Rank"]);
                }
                */
                string dataa = await HttpHelper.GetStringFromURL(string.Format("https://www.roblox.com/api/groups/{0}/RoleSets/", group.ID));
                JArray roles = JArray.Parse(dataa);

                group.Roles = new GroupRole[roles.Count];

                for (int i = 0; i < roles.Count; i++)
                {
                    JObject o = (JObject)roles[i];
                    group.Roles[i] = new GroupRole((string)o["Name"], (int)o["Rank"], (int)o["Id"]);
                }

                return group;
            }
            catch (WebException)
            {
                return null;
            }
        }

        private struct GroupResult_t
        {
#pragma warning disable 0649 // They actually do get set by using reflection.
            public bool FinalPage;
            public List<Group> Groups;
#pragma warning restore 0649
        }

        private async Task<GroupResult_t> GetAllyPage(int page)
        {
            string data = await HttpHelper.GetStringFromURL(string.Format("https://api.roblox.com/groups/{0}/allies?page={1}", ID, page));

            return JsonConvert.DeserializeObject<GroupResult_t>(data);
        }

        /// <summary>
        /// Gets a list of Allied Groups.
        /// </summary>
        /// <returns>List of allied groups.</returns>
        public async Task<Group[]> GetAllies()
        {
            int c = 1;
            List<Group> allies = new List<Group>();
            while(c < 1000)
            {
                GroupResult_t res = await GetAllyPage(c);
                Console.WriteLine("{0} {1}", res.FinalPage, c);
                foreach (Group g in res.Groups)
                    allies.Add(g);
                c++;
                if (res.FinalPage)
                    break;
            }
            return allies.ToArray();
        }

        private async Task<GroupResult_t> GetEnemyPage(int page)
        {
            string data = await HttpHelper.GetStringFromURL(string.Format("https://api.roblox.com/groups/{0}/enemies?page={1}", ID, page));

            return JsonConvert.DeserializeObject<GroupResult_t>(data);
        }

        /// <summary>
        /// Gets a list of Enemy Groups.
        /// </summary>
        /// <returns>List of Enemy groups.</returns>
        public async Task<Group[]> GetEnemies()
        {
            int c = 1;
            List<Group> allies = new List<Group>();
            while (c < 1000)
            {
                GroupResult_t res = await GetEnemyPage(c);
                allies.AddRange(res.Groups);
                c++;
                if (res.FinalPage)
                    break;
            }
            return allies.ToArray();
        }

        public static explicit operator int(Group group)
        {
            return group.ID;
        }

        public static explicit operator Group(int groupId)
        {
            return new Group(groupId);
        }

        /// <summary>
        /// Gets the role of a certain user in the group. If the user isn't in the group or the rank the user has doesn't exist within the group object, it returns null.
        /// </summary>
        /// <param name="user">The user to get the role from.</param>
        /// <returns>The role of the user.</returns>
        public async Task<GroupRole> GetRoleOfUser(User user)
        {
            string stringifiedJSON = await HttpHelper.GetStringFromURL(string.Format("https://groups.roblox.com/v1/users/{0}/groups/roles", user.ID));
            JObject obj = JObject.Parse(stringifiedJSON);
            JToken dataToken;

            if (obj.TryGetValue("data", out dataToken))
            {
                JArray arr = (JArray)dataToken;
                foreach (JObject groupMembershipObject in arr)
                {
                    JToken groupObject;
                    if (groupMembershipObject.TryGetValue("group", out groupObject))
                    {
                        JToken idToken;
                        if (((JObject)groupObject).TryGetValue("id", out idToken))
                        {
                            if (idToken.Value<int>() == ID)
                            {
                                JToken roleToken;
                                if (groupMembershipObject.TryGetValue("role", out roleToken))
                                {
                                    JObject roleObj = (JObject)roleToken;

                                    int roleId = 0;
                                    string roleName = "";
                                    int roleRank = 0;

                                    JToken tok;
                                    if(roleObj.TryGetValue("id", out tok))
                                    {
                                        roleId = tok.Value<int>();
                                    }
                                    else
                                    {
                                        throw new Exception("Token \"id\" could not be found in roleObj.");
                                    }

                                    if (roleObj.TryGetValue("rank", out tok))
                                    {
                                        roleRank = tok.Value<int>();
                                    }
                                    else
                                    {
                                        throw new Exception("Token \"rank\" could not be found in roleObj.");
                                    }

                                    if (roleObj.TryGetValue("name", out tok))
                                    {
                                        roleName = tok.Value<string>();
                                    }
                                    else
                                    {
                                        throw new Exception("Token \"name\" could not be found in roleObj.");
                                    }

                                    return new GroupRole(roleName, roleRank, roleId);
                                }
                                else
                                {
                                    throw new Exception("Token \"role\" could not be found in groupMembershipObject.");
                                }
                            }
                        }
                        else
                        {
                            throw new Exception("Token \"group.id\" could not be found in groupMembershipObject.");
                        }
                    }
                    else
                    {
                        throw new Exception("Token \"group\" could not be found in groupMembershipObject.");
                    }
                }
                return null;
            }
            else
            {
                throw new Exception("Could not find \"data\" in json response. Did the response model change?");
            }
        }

    }

    public class GroupRole
    {
        /// <summary>
        /// The name of the group role.
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// The rank number shown in the group manager.
        /// </summary>
        public int Rank
        {
            private set;
            get;
        }

        public int ID
        {
            private set;
            get;
        }

        internal GroupRole()
        {

        }

        internal GroupRole(string name, int rank, int id)
        {
            Name = name;
            Rank = rank;
            ID = id;
        }
    }
}
