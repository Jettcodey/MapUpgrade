// Copyright (c) 2025 Jettcodey
// Licensed under the MIT License
using System;
using System.Collections.Generic;
using UnityEngine;
using REPOLib.Modules;
using System.Text;
using Photon.Pun;

namespace Ardot.Jettcodey.REPO.MapUpgrade
{
	public class Upgrader : MonoBehaviourPunCallbacks
	{
		public ItemToggle _itemToggle;
		private PhotonView _photonView;

		private void Start()
		{
			_itemToggle = GetComponent<ItemToggle>();
			_photonView = GetComponent<PhotonView>();

			if (!StatsManager.instance.dictionaryOfDictionaries.ContainsKey("playerUpgradeMapUpgrade"))
			{
				StatsManager.instance.dictionaryOfDictionaries["playerUpgradeMapUpgrade"] = new Dictionary<string, int>();
			}
		}

		public static void UpdateStat(int amount, string steamId, string stat)
		{
			if (!StatsManager.instance.dictionaryOfDictionaries.ContainsKey(stat))
			{
				StatsManager.instance.dictionaryOfDictionaries[stat] = new Dictionary<string, int>();
			}

			Dictionary<string, int> dictionary = StatsManager.instance.dictionaryOfDictionaries[stat];
			if (!dictionary.ContainsKey(steamId))
			{
				dictionary[steamId] = 0;
			}
			dictionary[steamId] += amount;

			Plugin.Update_MapUpgrade();
		}

		public static int GetStat(string steamId, string upgradeName)
		{
			if (!StatsManager.instance.dictionaryOfDictionaries.ContainsKey(upgradeName))
			{
				return 0;
			}

			Dictionary<string, int> dictionary = StatsManager.instance.dictionaryOfDictionaries[upgradeName];
			int value;
			return dictionary.TryGetValue(steamId, out value) ? value : 0;
		}

		public void UpgradeMapUpgrade()
		{
			PlayerAvatar player = SemiFunc.PlayerAvatarGetFromPhotonID(_itemToggle.playerTogglePhotonID);
			if (player == null)
				return;

			string steamId = SemiFunc.PlayerGetSteamID(player);

			Plugin.Logger.LogInfo($"Your Map Upgrade level before: {GetStat(steamId, "playerUpgradeMapUpgrade")}");

			UpdateStat(1, steamId, "playerUpgradeMapUpgrade");

			if (SemiFunc.IsMultiplayer() && Utils.IsHost() && _photonView != null)
			{
				_photonView.RPC(nameof(SyncUpgradeRPC), RpcTarget.Others, steamId, 1);
			}

			Plugin.Logger.LogInfo($"Your Map Upgrade level after: {GetStat(steamId, "playerUpgradeMapUpgrade")}");
		}

		// not tested this at all
		[PunRPC]
		private void SyncUpgradeRPC(string steamId, int amount)
		{
			if (!StatsManager.instance.dictionaryOfDictionaries.ContainsKey("playerUpgradeMapUpgrade"))
			{
				StatsManager.instance.dictionaryOfDictionaries["playerUpgradeMapUpgrade"] = new Dictionary<string, int>();
			}

			Dictionary<string, int> dictionary = StatsManager.instance.dictionaryOfDictionaries["playerUpgradeMapUpgrade"];
			if (!dictionary.ContainsKey(steamId))
			{
				dictionary[steamId] = 0;
			}
			dictionary[steamId] += amount;

			Plugin.Update_MapUpgrade();
		}
	}
}