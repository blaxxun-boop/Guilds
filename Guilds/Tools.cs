using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace Guilds;

public static class Tools
{
	internal static Texture2D loadTexture(string name)
	{
		Texture2D texture = new(0, 0);
		texture.LoadImage(ReadEmbeddedFileBytes("Icons." + name));
		return texture;
	}

	internal static Sprite loadSprite(string name, int width, int height) => Sprite.Create(loadTexture(name), new Rect(0, 0, width, height), Vector2.zero);

	public static byte[] ReadEmbeddedFileBytes(string name)
	{
		using MemoryStream stream = new();
		Assembly.GetExecutingAssembly().GetManifestResourceStream("Guilds." + name)?.CopyTo(stream);
		return stream.ToArray();
	}

	internal static AssetBundle LoadAssetBundle(string bundleName)
	{
		string resource = typeof(Guilds).Assembly.GetManifestResourceNames().Single(s => s.EndsWith(bundleName));
		return AssetBundle.LoadFromStream(typeof(Guilds).Assembly.GetManifestResourceStream(resource));
	}

	internal static Sprite LoadNewSprite(string FilePath, float PixelsPerUnit = 100.0f, SpriteMeshType spriteType = SpriteMeshType.Tight)
	{
		Texture2D spriteTexture = LoadTexture(FilePath)!;
		return Sprite.Create(spriteTexture, new Rect(0, 0, spriteTexture.width, spriteTexture.height), new Vector2(0, 0), PixelsPerUnit, 0, spriteType);
	}

	internal static Sprite ConvertTextureToSprite(Texture2D texture, float PixelsPerUnit = 100.0f, SpriteMeshType spriteType = SpriteMeshType.Tight)
	{
		return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0), PixelsPerUnit, 0, spriteType);
	}

	private static Texture2D? LoadTexture(string FilePath)
	{
		if (File.Exists(FilePath))
		{
			byte[] FileData = File.ReadAllBytes(FilePath);
			Texture2D Tex2D = new(2, 2);
			if (Tex2D.LoadImage(FileData))
			{
				return Tex2D;
			}
		}
		return null;
	}
	
	internal static string GetHumanFriendlyTime(int seconds)
	{
		TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);

		if (timeSpan.TotalSeconds < 60)
		{
			return Localization.instance.Localize("$guilds_less_than_minute");
		}

		string daysText = Localization.instance.Localize(timeSpan.Days >= 2 ? "$guilds_day_plural" : "$guilds_day_singular", ((int)timeSpan.TotalDays).ToString());
		if (timeSpan.TotalDays >= 30)
		{
			return daysText;
		}

		string hoursText = Localization.instance.Localize(timeSpan.Hours >= 2 ? "$guilds_hour_plural" : "$guilds_hour_singular", timeSpan.Hours.ToString());
		if (timeSpan.TotalDays >= 1)
		{
			return timeSpan.Hours == 0 ? daysText : Localization.instance.Localize("$guilds_bind_day_hour", daysText, hoursText);
		}
		
		string minutesText = Localization.instance.Localize(timeSpan.Minutes >= 2 ? "$guilds_minute_plural" : "$guilds_minute_singular", timeSpan.Minutes.ToString());
		return timeSpan.TotalHours >= 1 ? timeSpan.Minutes == 0 ? hoursText : Localization.instance.Localize("$guilds_bind_hour_minute", hoursText, minutesText) : minutesText;
	}

	internal static bool ValidateChar(char ch) => char.IsLetterOrDigit(ch) || "@!#$%&'*+-=?^_{|}~ ".IndexOf(ch) != -1;
	
	internal class NameValidator : TMP_InputValidator
	{
		public override char Validate(ref string text, ref int pos, char ch)
		{
			if (ValidateChar(ch))
			{
				text = text.Insert(pos++, ch.ToString());
				return ch;
			}

			return '\0';
		}
	}
	
	public static List<Player> GetNearbyGuildMembers(Player player, float range, bool includeSelf = false)
	{
		if (API.GetPlayerGuild(player) is not { } guild)
		{
			return new List<Player>();
		}
		List<PlayerReference> guildPlayers = API.GetOnlinePlayers(guild).ToList();
		List<Player> nearbyPlayers = new();
		Player.GetPlayersInRange(player.transform.position, range, nearbyPlayers);
		nearbyPlayers.RemoveAll(p => !guildPlayers.Contains(PlayerReference.fromPlayer(p)) || (!includeSelf && p == player));
		return nearbyPlayers;
	}
}
