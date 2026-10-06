// Originally developed by Ardot66
// Modified and maintained by Jettcodey
// Licensed under the MIT License
using System.Collections.Generic;
using Photon.Pun;
using REPOLib.Modules;
using UnityEngine;

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

            if (
                !StatsManager.instance.dictionaryOfDictionaries.ContainsKey(
                    "playerUpgradeMapUpgrade"
                )
            )
            {
                StatsManager.instance.dictionaryOfDictionaries["playerUpgradeMapUpgrade"] =
                    new Dictionary<string, int>();
            }
        }

        public static void UpdateStat(int amount, string steamId, string stat)
        {
            if (!StatsManager.instance.dictionaryOfDictionaries.ContainsKey(stat))
            {
                StatsManager.instance.dictionaryOfDictionaries[stat] =
                    new Dictionary<string, int>();
            }

            Dictionary<string, int> dictionary = StatsManager.instance.dictionaryOfDictionaries[
                stat
            ];
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
                return 0;
            Dictionary<string, int> dictionary = StatsManager.instance.dictionaryOfDictionaries[
                upgradeName
            ];
            return dictionary.TryGetValue(steamId, out int value) ? value : 0;
        }

        public void UpgradeMapUpgrade()
        {
            PlayerAvatar player = SemiFunc.PlayerAvatarGetFromPhotonID(
                _itemToggle.playerTogglePhotonID
            );
            if (player == null)
                return;

            string steamId = SemiFunc.PlayerGetSteamID(player);

            if (Utils.IsHost())
            {
                Plugin.Logger.LogInfo($"Host executing map upgrade for {player.playerName}");
                UpdateStat(1, steamId, "playerUpgradeMapUpgrade");

                if (SemiFunc.IsMultiplayer() && _photonView != null)
                {
                    _photonView.RPC(nameof(SyncUpgradeRPC), RpcTarget.Others, steamId, 1);
                }

                if (_photonView != null && _photonView.IsMine)
                {
                    PhotonNetwork.Destroy(gameObject);
                }
            }
            else if (_photonView != null && _photonView.IsMine)
            {
                Plugin.Logger.LogInfo($"Client requesting map upgrade from host...");
                _photonView.RPC(nameof(RequestMapUpgradeRPC), RpcTarget.MasterClient, steamId);
            }
        }

        [PunRPC]
        public void RequestMapUpgradeRPC(string steamId)
        {
            if (!Utils.IsHost())
                return;

            Plugin.Logger.LogInfo($"Host received map upgrade request for {steamId}");
            UpdateStat(1, steamId, "playerUpgradeMapUpgrade");

            if (SemiFunc.IsMultiplayer() && _photonView != null)
            {
                _photonView.RPC(nameof(SyncUpgradeRPC), RpcTarget.Others, steamId, 1);
            }

            if (_photonView != null && _photonView.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }

        [PunRPC]
        private void SyncUpgradeRPC(string steamId, int amount)
        {
            if (Utils.IsHost())
                return;

            Plugin.Logger.LogInfo($"Client received sync for map upgrade: {steamId}");
            UpdateStat(amount, steamId, "playerUpgradeMapUpgrade");
        }
    }
}
