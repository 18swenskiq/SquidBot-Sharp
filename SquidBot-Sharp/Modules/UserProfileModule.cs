using DSharpPlus.Entities;
using SquidBot_Sharp.Models;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace SquidBot_Sharp.Modules
{
    public class UserProfileModule
    {

        public UserProfile CheckIfUserProfileExists(DiscordUser UserInfo)
        {
            var userprofilepath = Path.Combine(Directory.GetCurrentDirectory(), "\\UserProfiles\\", $"{UserInfo.Id}.userdata");
            if (!File.Exists(userprofilepath)) return null;
            return DeserializeProfile(UserInfo.Id);
        }
        public UserProfile CheckIfUserProfileExists(ulong UserID)
        {
            var userprofilepath = Path.Combine("datafiles\\UserProfiles\\", $"{UserID}.userdata");
            if (!File.Exists(userprofilepath)) return null;
            return DeserializeProfile(UserID);
        }

        public bool ModifyUserProfile(UserProfile updatedProfile)
        {
            var deserializeduserprofile = DeserializeProfile(updatedProfile.UserID);
            if(deserializeduserprofile == null)
            {
                return false;
            }
            var serializeit = SerializeProfile(updatedProfile);
            if (!serializeit) return false;
            return true;
        }

        public UserProfile BuildUserProfile(TimeZoneInfo timezone, DiscordUser UserInfo)
        {
            var userprofile = new UserProfile { TimeZone = timezone, UserID = UserInfo.Id, Version = 1, ProfileName = UserInfo.Username };
            bool serializeresult = SerializeProfile(userprofile);
            if (!serializeresult) return null;
            return userprofile;
        }


        public bool SerializeProfile(UserProfile userProfile)
        {
            try
            {
                using (Stream stream = File.Open(@$"datafiles\\UserProfiles\\{userProfile.UserID}.userdata", FileMode.Create))
                {
                    BinaryFormatter bin = new BinaryFormatter();
                    bin.Serialize(stream, userProfile);
                }
                return true;
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }

        public UserProfile DeserializeProfile(UserProfile userProfile)
        {
            try
            {
                using (Stream stream = File.Open($"datafiles/UserProfiles/{userProfile.UserID}.userdata", FileMode.Open))
                {
                    BinaryFormatter bin = new BinaryFormatter();
                    var userProfileDeserialized = (UserProfile)bin.Deserialize(stream);
                    return userProfileDeserialized;
                }
            }
            catch
            {
                return null;
            }
        }

        public UserProfile DeserializeProfile(ulong userID)
        {
            try
            {
                using (Stream stream = File.Open($"datafiles/UserProfiles/{userID}.userdata", FileMode.Open))
                {
                    BinaryFormatter bin = new BinaryFormatter();
                    var userProfileDeserialized = (UserProfile)bin.Deserialize(stream);
                    return userProfileDeserialized;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
