﻿#region References

using System;
using System.Linq;
using Scribe.Data;

#endregion

namespace Scribe.Website.Providers
{
	public class RoleProvider : System.Web.Security.RoleProvider
	{
		#region Properties

		public override string ApplicationName { get; set; }

		#endregion

		#region Methods

		public override void AddUsersToRoles(string[] usernames, string[] roleNames)
		{
			throw new NotImplementedException();
		}

		public override void CreateRole(string roleName)
		{
			throw new NotImplementedException();
		}

		public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
		{
			throw new NotImplementedException();
		}

		public override string[] FindUsersInRole(string roleName, string usernameToMatch)
		{
			throw new NotImplementedException();
		}

		public override string[] GetAllRoles()
		{
			throw new NotImplementedException();
		}

		public override string[] GetRolesForUser(string username)
		{
			using (var context = new ScribeContext())
			{
				var userId = username.Split(';').First().ConvertToInt();
				var user = context.Users.FirstOrDefault(x => x.Id == userId);
				return user?.Tags?.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToArray() ?? new string[0];
			}
		}

		public override string[] GetUsersInRole(string roleName)
		{
			throw new NotImplementedException();
		}

		public override bool IsUserInRole(string username, string roleName)
		{
			using (var context = new ScribeContext())
			{
				var userId = username.Split(';').First().ConvertToInt();
				var user = context.Users.FirstOrDefault(x => x.Id == userId);
				return user != null && user.InRole(roleName);
			}
		}

		public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
		{
			throw new NotImplementedException();
		}

		public override bool RoleExists(string roleName)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}