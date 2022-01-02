using Newtonsoft.Json;
using Oxide.Core.Configuration;
using Oxide.Core.CSharp;
using Oxide.Core.Database;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Libraries;
using Oxide.Core.MySql;
using Oxide.Core.Plugins;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using Rust;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text;
using System;
using UnityEngine.UI;
using UnityEngine;

namespace Oxide.Plugins {
    [Info("SQLLogger", "FiremanDave", "1.1.36"/*, ResourceId = 0*/)]
    [Description("Logs all kinds of stuff to MySql")]
    
    public class SQLLogger : RustPlugin { 	
		private Dictionary<uint, StorageType> itemTracker = new Dictionary<uint, StorageType>();
    	private Dictionary<BasePlayer, Int32> loginTime = new Dictionary<BasePlayer, int>();
		readonly Core.MySql.Libraries.MySql _mySql = new Core.MySql.Libraries.MySql();
        private Connection _mySqlConnection = null;

		// Configuration absent config file
		protected override void LoadDefaultConfig() {
            PrintWarning("Creating a new configuration file");
            Config.Clear();
            Config["Host"] = "";
            Config["Database"] = "";
            Config["Port"] = 3306;
            Config["Username"] = "";
            Config["Password"] = "";
            Config["_AdminLogWords"] = "admin, admn, fuck, bitch, cunt, cock";
            Config["Version"] = "1.1.36";     
            SaveConfig();
        }

        // Create MySQL connection to specified server
        private void StartConnection() {
        	try {
        		Puts("Opening connection.");
	                _mySqlConnection = _mySql.OpenDb(Config["Host"].ToString(), Convert.ToInt32(Config["Port"]), Config["Database"].ToString(), Config["Username"].ToString(), Config["Password"].ToString(), this);
	                Puts("Connection opened.");
        	}
        	catch (Exception ex)
        	{
        		Puts(ex.Message);
        	}
        }
        
        // Build the executeQuery command 
        public void executeQuery(string query, params object[] data) {
            var sql = Sql.Builder.Append(query, data);
            _mySql.Insert(sql, _mySqlConnection);
        }

		// **************************************
		// *			Table Creation			*
		// **************************************
        private void createTablesOnConnect() {	
			try {
				// Create player stats table
            	// executeQuery("CREATE TABLE IF NOT EXISTS player_stats (id INT(11) NOT NULL AUTO_INCREMENT, player_id BIGINT(20) NULL, player_name VARCHAR(255) NULL, player_ip VARCHAR(128) NULL, player_state INT(1) NULL DEFAULT '0', player_online_time BIGINT(20) DEFAULT '0', player_last_login TIMESTAMP NULL, PRIMARY KEY (`player_id`), UNIQUE (`player_id`) ) ENGINE=InnoDB;");   
				// Create player resources gather table
            	executeQuery("CREATE TABLE IF NOT EXISTS player_resource_gather (id INT(11) NOT NULL AUTO_INCREMENT, player_id BIGINT(20) NULL, player_name VARCHAR(255) NULL, resource VARCHAR(255) NULL, amount INT(32), date TIMESTAMP NULL, location VARCHAR(255) NULL, activity VARCHAR(255) NULL, PRIMARY KEY (`id`), UNIQUE KEY `PlayerGather` (`player_id`,`resource`,`date`) ) ENGINE=InnoDB;");
				// Create player crafted items table
            	executeQuery("CREATE TABLE IF NOT EXISTS player_crafted_item (id INT(11) NOT NULL AUTO_INCREMENT, player_id BIGINT(20) NULL, player_name VARCHAR(255) NULL, item VARCHAR(128), amount INT(32), date TIMESTAMP NULL, PRIMARY KEY (`id`), UNIQUE KEY `PlayerItem` (`player_id`,`item`,`date`) ) ENGINE=InnoDB;");
				// Create player animal kills table
            	executeQuery("CREATE TABLE IF NOT EXISTS player_kill_animal	(id INT(11) NOT NULL AUTO_INCREMENT, player_id BIGINT(20) NULL, animal VARCHAR(128), distance INT(11) NULL DEFAULT '0', weapon VARCHAR(128) NULL, time TIMESTAMP NULL, location VARCHAR(255) NULL, PRIMARY KEY (`id`) ) ENGINE=InnoDB;");
				// Create player kills table
            	executeQuery("CREATE TABLE IF NOT EXISTS player_kill (id INT(11) NOT NULL AUTO_INCREMENT, killer_id BIGINT(20) NULL, killer_name VARCHAR(255) NULL, victim_id BIGINT(20) NULL, victim_name VARCHAR(255) NULL, bodypart VARCHAR(128), weapon VARCHAR(128), distance INT(11) NULL, time TIMESTAMP NULL, viclocation VARCHAR(255) NULL, killerlocation VARCHAR(255) NULL, PRIMARY KEY (`id`) ) ENGINE=InnoDB;");
				// Create player deaths table
            	executeQuery("CREATE TABLE IF NOT EXISTS player_death (id INT(11) NOT NULL AUTO_INCREMENT, player_id BIGINT(20) NULL, player_name VARCHAR(255) NULL, cause VARCHAR(128), count INT(11) NULL DEFAULT '1', date TIMESTAMP NULL, time TIMESTAMP NULL, PRIMARY KEY (`id`), UNIQUE (`player_id`,`date`,`cause`) ) ENGINE=InnoDB;");
				// Create player building destruction table
            	executeQuery("CREATE TABLE IF NOT EXISTS player_destroy_building(id INT(11) NOT NULL AUTO_INCREMENT, player_id BIGINT(20) NULL, player_name VARCHAR(255) NULL, building VARCHAR(128), building_grade VARCHAR(128), weapon VARCHAR(128), location VARCHAR(255) NULL, time TIMESTAMP NULL, PRIMARY KEY (`id`) ) ENGINE=InnoDB;");
				// Create player built building tables
            	executeQuery("CREATE TABLE IF NOT EXISTS player_place_building (id INT(11) NOT NULL AUTO_INCREMENT, player_id BIGINT(20) NULL, player_name VARCHAR(128) NULL, building VARCHAR(128) NULL, grade VARCHAR(255) NULL, location VARCHAR(255) NULL DEFAULT '1', date TIMESTAMP NULL, PRIMARY KEY (`id`), UNIQUE (`player_id`,`date`,`building`) ) ENGINE=InnoDB;");
				// Create player deployables table
            	executeQuery("CREATE TABLE IF NOT EXISTS player_place_deployable(id INT(11) NOT NULL AUTO_INCREMENT, player_id BIGINT(20) NULL, player_name VARCHAR(128) NULL, deployable VARCHAR(128) NULL, amount INT(32) NULL DEFAULT '1', date TIMESTAMP NULL, location VARCHAR(255) NULL, PRIMARY KEY (`id`), UNIQUE (`player_id`,`date`,`deployable`) ) ENGINE=InnoDB;");
				// Create TC auth list table
				executeQuery("CREATE TABLE IF NOT EXISTS player_authorize_list (id INT(11) NOT NULL AUTO_INCREMENT, player_id BIGINT(20) NULL, player_name VARCHAR(128) NULL, cupboard VARCHAR(128) NULL, location VARCHAR(128) NULL, access VARCHAR(255) NULL DEFAULT '0', time VARCHAR(255) NULL, PRIMARY KEY (`id`), UNIQUE (`Cupboard`) ) ENGINE=InnoDB;");
				// Create player chat commands table
				executeQuery("CREATE TABLE IF NOT EXISTS player_chat_command (id INT(11) NOT NULL AUTO_INCREMENT, player_id BIGINT(20) NULL, player_name VARCHAR(128) NULL, command VARCHAR(128) NULL, text VARCHAR(255) NULL, time TIMESTAMP NULL, PRIMARY KEY (`id`) ) ENGINE=InnoDB;");
				// Create player connection log table
				executeQuery("CREATE TABLE IF NOT EXISTS player_connect_log	(id INT(11) NOT NULL AUTO_INCREMENT, player_id BIGINT(20) NULL, player_name VARCHAR(128) NULL, state VARCHAR(128) NULL, time TIMESTAMP NULL, PRIMARY KEY (`id`) ) ENGINE=InnoDB;");
				// Create chat log table
				executeQuery("CREATE TABLE IF NOT EXISTS server_log_chat (id INT(11) NOT NULL AUTO_INCREMENT, player_id BIGINT(20) NULL, player_name VARCHAR(128) NULL, chat_message VARCHAR(255), time TIMESTAMP NULL, PRIMARY KEY (`id`) ) ENGINE=InnoDB;");
				// Create server console log table
				executeQuery("CREATE TABLE IF NOT EXISTS server_log_console (id INT(11) NOT NULL AUTO_INCREMENT, server_message VARCHAR(255) NULL, time TIMESTAMP NULL, PRIMARY KEY (`id`) ) ENGINE=InnoDB;");
				// Create admin log table
				executeQuery("CREATE TABLE IF NOT EXISTS admin_log	(id INT(11) NOT NULL AUTO_INCREMENT, player_id BIGINT(20) NULL, player_name VARCHAR(128) NULL, command VARCHAR(128) NULL, text VARCHAR(255) NULL, time TIMESTAMP NULL, PRIMARY KEY (`id`) ) ENGINE=InnoDB;");
				// Create player hacks table
				executeQuery("CREATE TABLE IF NOT EXISTS player_hacks (id INT(11) NOT NULL AUTO_INCREMENT, player_id BIGINT(20) NULL, player_name VARCHAR(255) NULL, location VARCHAR(128) NULL, activity VARCHAR(255) NULL, date TIMESTAMP NULL, PRIMARY KEY (`id`) ) ENGINE=InnoDB;");
				// Create lock activity table
				executeQuery("CREATE TABLE IF NOT EXISTS lock_activity (id INT(11) NOT NULL AUTO_INCREMENT, player_id BIGINT(20) NULL, player_name VARCHAR(255) NULL, location VARCHAR(128) NULL, value VARCHAR(128) NULL, PRIMARY KEY (`id`) ) ENGINE=InnoDB;");
				// Create trap activity table
				executeQuery("CREATE TABLE IF NOT EXISTS trap_activity (id INT(11) NOT NULL AUTO_INCREMENT, player_id BIGINT(20) NULL, player_name VARCHAR(255) NULL, location VARCHAR(128) NULL, activity VARCHAR(255) NULL, time TIMESTAMP NULL, PRIMARY KEY (`id`) ) ENGINE=InnoDB;");
				// Create door logging table
				executeQuery("CREATE TABLE IF NOT EXISTS door_activity (id INT(11) NOT NULL AUTO_INCREMENT, player_id BIGINT(20) NULL, player_name VARCHAR(255) NULL, location VARCHAR(128) NULL, activity VARCHAR(255) NULL, PRIMARY KEY (`id`) ) ENGINE=InnoDB;");
				// Create recycler table
				executeQuery("CREATE TABLE IF NOT EXISTS recycler_activity (id INT(11) NOT NULL AUTO_INCREMENT, player_id BIGINT(20) NULL, player_name VARCHAR(255) NULL, item_quantity INT(11) NULL, item_description VARCHAR(255) NULL, location VARCHAR(128) NULL, time TIMESTAMP NULL, PRIMARY KEY (`id`) ) ENGINE=InnoDB;");
				// Create research table
				executeQuery("CREATE TABLE IF NOT EXISTS research_activity (id INT(11) NOT NULL AUTO_INCREMENT, player_id BIGINT(20) NULL, player_name VARCHAR(255) NULL, item_researched VARCHAR(255) NULL, research_location VARCHAR(255) NULL, time TIMESTAMP NULL, PRIMARY KEY (`id`) ) ENGINE=InnoDB;");
				// Create vending machine table
				executeQuery("CREATE TABLE IF NOT EXISTS vending_activity (id INT(11) NOT NULL AUTO_INCREMENT, player_id BIGINT(20) NULL, player_name VARCHAR(255) NULL, item_purchased VARCHAR(255) NULL, purchase_price VARCHAR(255) NULL, location VARCHAR(255) NULL, PRIMARY KEY (`id`) ) ENGINE=InnoDB;");
				// Create mount table
				executeQuery("CREATE TABLE IF NOT EXISTS mount_activity (id INT(11) NOT NULL AUTO_INCREMENT,player_id BIGINT(20) NULL,  player_name VARCHAR(255) NULL, mount_description VARCHAR(255) NULL, activity VARCHAR(255) NULL, location VARCHAR(255) NULL, PRIMARY KEY (`id`) ) ENGINE=InnoDB;");
				// Create RP table
				executeQuery("CREATE TABLE IF NOT EXISTS rewards_activity (id INT(11) NOT NULL AUTO_INCREMENT, player_id BIGINT(20) NULL, player_name VARCHAR(255) NULL, activity VARCHAR(255) NULL, PRIMARY KEY (`id`) ) ENGINE=InnoDB;");
				// Create building upgrade and repair table
				executeQuery("CREATE TABLE IF NOT EXISTS upgrade_repair_activity (id INT(11) NOT NULL AUTO_INCREMENT, player_id BIGINT(20) NULL, player_name VARCHAR(255) NULL, item_type VARCHAR(255) NULL, grade VARCHAR(255) NULL, location VARCHAR(255) NULL, activity VARCHAR(255) NULL, date VARCHAR(255) NULL, PRIMARY KEY (`id`) ) ENGINE=InnoDB;");
				// Create sign activity table
				executeQuery("CREATE TABLE IF NOT EXISTS sign_activity (id INT(11) NOT NULL AUTO_INCREMENT, player_id BIGINT(20) NULL, player_name VARCHAR(255) NULL, sign_type VARCHAR(255) NULL, location VARCHAR(255) NULL, PRIMARY KEY (`id`) ) ENGINE=InnoDB;");
				// Create stash table
				executeQuery("CREATE TABLE IF NOT EXISTS stash_activity (id INT(11) NOT NULL AUTO_INCREMENT, player_id BIGINT(20) NULL, player_name VARCHAR(255) NULL, location VARCHAR(255) NULL, activity VARCHAR(255) NULL, PRIMARY KEY (`id`) ) ENGINE=InnoDB;");
				// Loot added table
				executeQuery("CREATE TABLE IF NOT EXISTS loot_added (id INT(11) NOT NULL AUTO_INCREMENT, player_id BIGINT(20) NULL, player_name VARCHAR(255) NULL, container_name VARCHAR(255) NULL, owner_id BIGINT(20) NULL,  chest_owner VARCHAR(255) NULL, location VARCHAR(255) NULL, date TIMESTAMP NULL, item VARCHAR(255) NULL, quantity VARCHAR(255) NULL, PRIMARY KEY (`id`) ) ENGINE=InnoDB;");
				// Loot taken table
				executeQuery("CREATE TABLE IF NOT EXISTS loot_removed (id INT(11) NOT NULL AUTO_INCREMENT, player_id BIGINT(20) NULL, player_name VARCHAR(255) NULL, container_name VARCHAR(255) NULL, owner_id BIGINT(20) NULL,  chest_owner VARCHAR(255) NULL, location VARCHAR(255) NULL, date TIMESTAMP NULL, item VARCHAR(255) NULL, quantity VARCHAR(255) NULL, PRIMARY KEY (`id`) ) ENGINE=InnoDB;");
				// Create turrets table
				executeQuery("CREATE TABLE IF NOT EXISTS turret_activity (id INT(11) NOT NULL AUTO_INCREMENT, player_id BIGINT(20) NULL, player_name VARCHAR(255) NULL, turret_id VARCHAR(255) NULL, mode VARCHAR(255) NULL, location VARCHAR(255) NULL, PRIMARY KEY (`id`) ) ENGINE=InnoDB;");
				// Brad and Patrol Heli kills
				executeQuery("CREATE TABLE IF NOT EXISTS fun_kills (id INT(11) NOT NULL AUTO_INCREMENT, player_id BIGINT(20) NULL, player_name VARCHAR(255) NULL, vehicle VARCHAR(255) NULL, location VARCHAR(255) NULL, time VARCHAR(255) NULL, PRIMARY KEY (`id`) ) ENGINE=InnoDB;");
			}
            catch (Exception ex) {
                Puts(ex.ToString());
            }
        }
		
		
		// **************************************
		// *			Plugin Startup			*
		// **************************************
        void Init() {
            #if !RUST
            throw new NotSupportedException("This plugin does not support this game");
            #endif

       		string curVersion = Version.ToString();
            string[] version = curVersion.Split('.');
            var majorPluginUpdate = version[0]; 	// Big Plugin Update
            var minorPluginUpdate = version[1];		// Small Plugin Update
            var databaseVersion = version[2];		// Database Update
        	var pluginVersion = majorPluginUpdate+"."+minorPluginUpdate;
        	Puts("Plugin version: "+majorPluginUpdate+"."+minorPluginUpdate+"  Database version: "+databaseVersion);
        	if (pluginVersion != getConfigVersion("plugin") ) {
        		Puts("New "+pluginVersion+" Old "+getConfigVersion("plugin") );
        		Config["Version"] = pluginVersion+"."+databaseVersion;     
	            SaveConfig();	 
        	}
        	if (databaseVersion != getConfigVersion("db") ){
        		Puts("New "+databaseVersion+" Old "+getConfigVersion("db") );
        		PrintWarning("Database base changes please drop the old!");
        		Config["Version"] = pluginVersion+"."+databaseVersion;    
	            SaveConfig();	           		
        	}
		}   

        // Plugin loaded
        void Loaded() {
        	StartConnection();
        	createTablesOnConnect();
        	// foreach (var player in BasePlayer.activePlayerList) {
                // OnPlayerInit(player);
            // }           
        }

        // Plugin unloaded
        void Unloaded() {
            // foreach (var player in BasePlayer.activePlayerList)
            // {
                // OnPlayerDisconnected(player);
            // }
            timer.Once(5, () =>
            {
                _mySql.CloseDb(_mySqlConnection);
                _mySqlConnection = null;
            });
        }

		// **************************************
		// *			Player Hooks			*
		// **************************************

        // // Player login
        // void OnPlayerInit(BasePlayer player) {
            // if (!player.IsConnected)
                // return;

            // string properName = EncodeNonAsciiCharacters(player.displayName);

            // executeQuery("INSERT INTO player_stats (player_id, player_name, player_ip, player_state, player_last_login) VALUES (@0, @1, @2, 1, @3) ON DUPLICATE KEY UPDATE player_name = @1, player_ip = @2, player_state = 1, player_last_login= @3", player.userID, properName, player.net.connection.ipaddress, getDateTime());
            // if(loginTime.ContainsKey(player))
                // OnPlayerDisconnected(player);

            // loginTime.Add(player, (Int32) (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds);
		    // executeQuery("INSERT INTO player_connect_log (player_id, player_name, state, time) VALUES (@0, @1, @2, @3)", player.userID, EncodeNonAsciiCharacters(player.displayName),"Connected", getDateTime());
        // }

		// // Player Logout
		// void OnPlayerDisconnected(BasePlayer player) {
			// if(loginTime.ContainsKey(player)) {
		    	// executeQuery("UPDATE player_stats SET player_online_time = player_online_time + @0, player_state = 0 WHERE player_id = @1", (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds - loginTime[player] player.userID);
				// loginTime.Remove(player);
			// }
		    // executeQuery("INSERT INTO player_connect_log (player_id, player_name, state, time) VALUES (@0, @1, @2, @3)", player.userID, EncodeNonAsciiCharacters(player.displayName),"Disconnected", getDateTime());
		// }

		// Player Gather resource
        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item) 
		{
            if(entity is BasePlayer) 
			{		
            	string properName = EncodeNonAsciiCharacters(((BasePlayer)entity).displayName);
                executeQuery("INSERT INTO player_resource_gather (player_id, resource, amount, date, player_name, location, activity) VALUES (@0, @1, @2, @3, @4, @5, @6) ON DUPLICATE KEY UPDATE amount = amount +"+item.amount, ((BasePlayer)entity).userID, item.info.displayName.english, item.amount, getDateTime(), properName, EntityPosition(entity), "Resource gathered" );
			}
        }

        // Player Pickup resource
        void OnCollectiblePickup(Item item, BasePlayer player) 
		{
        	string properName = EncodeNonAsciiCharacters(((BasePlayer)player).displayName);
            executeQuery("INSERT INTO player_resource_gather (player_id, resource, amount, date, player_name, location, activity) VALUES (@0, @1, @2, @3, @4, @5, @6) ON DUPLICATE KEY UPDATE amount = amount +"+item.amount, ((BasePlayer)player).userID, item.info.displayName.english, item.amount, getDateTime(), properName, EntityPosition(player), "Resource picked up" );
        }

		// Player item pickup
		void OnItemAction(Item item, string action, BasePlayer player)
		{
			executeQuery("INSERT INTO player_resource_gather (player_id, resource, amount, date, player_name, location, activity) VALUES (@0, @1, @2, @3, @4, @5, @6) ON DUPLICATE KEY UPDATE amount = amount +"+item.amount, ((BasePlayer)player).userID, item.info.displayName.english, item.amount, getDateTime(), ((BasePlayer)player).displayName.ToString(), EntityPosition(player), "Item picked up" );
		}
		
		// Player drops item
		void OnItemAction(Item item, string action)
		{
			var player = item.parent.playerOwner;
            if (action.ToLower() != "drop" || player == null) return;
			executeQuery("INSERT INTO player_resource_gather (player_id, resource, amount, date, player_name, location, activity) VALUES (@0, @1, @2, @3, @4, @5, @6) ON DUPLICATE KEY UPDATE amount = amount +"+item.amount, ((BasePlayer)player).userID, item.info.displayName.english, item.amount, getDateTime(), ((BasePlayer)player).displayName.ToString(), EntityPosition(player), "Item dropped" );
		}

        // Player crafted item
        void OnItemCraftFinished(ItemCraftTask task, Item item) 
		{
            executeQuery("INSERT INTO player_crafted_item (player_id, player_name, item, amount, date) VALUES (@0, @1, @2, @3, @4) ON DUPLICATE KEY UPDATE amount = amount +"+item.amount, task.owner.userID, PlayerName(task.owner), item.info.displayName.english, item.amount, getDateTime() );
        }

        // Player place item or building
		void OnEntityBuilt(Planner plan, GameObject go, HeldEntity heldentity, BuildingGrade.Enum grade) 
		{
			string name = plan.GetOwnerPlayer().displayName; //Playername
			ulong playerID = plan.GetOwnerPlayer().userID; //steam_id
			var placedObject = go.ToBaseEntity();
		    if (placedObject is BuildingBlock) 
			{
                string item_name = ((BuildingBlock)placedObject).blockDefinition.info.name.english;
				string item_grade = ((BuildingBlock)placedObject).currentGrade.gradeBase.name;
                executeQuery("INSERT INTO player_place_building (player_id, player_name, building, grade, location, date) VALUES (@0, @1, @2, @3, @4, @5)", playerID, name, placedObject, item_grade, EntityPosition(placedObject), getDateTime() );
            }
			else if (plan.isTypeDeployable) 
			{
            	string item_name = plan.GetOwnerItemDefinition().displayName.english;
                executeQuery("INSERT INTO player_place_deployable (player_id, player_name, deployable, date, location) VALUES (@0, @1, @2, @3, @4)", playerID, name, item_name, getDateTime(), EntityPosition(placedObject) );
            }
			if (plan.GetOwnerItemDefinition().shortname == "cupboard.tool")
            {
                var cupboard = go.GetComponent<BuildingPrivlidge>();
                BasePlayer player = plan.GetOwnerPlayer();
                OnCupboardAuthorize(cupboard, player);
                OnCupboardAuthorize(cupboard, player); // Dirty fix for set access to 1
            }
		}
		
		// Upgrade and repair buildings
		void OnStructureUpgrade(BaseCombatEntity entity, BasePlayer player, BuildingGrade.Enum grade)
		{
			executeQuery("INSERT INTO upgrade_repair_activity (player_name, item_type, grade, location, activity, date) VALUES (@0, @1, @2, @3, @4, @5)", ((BasePlayer) player).displayName.ToString(), entity.ShortPrefabName, grade, EntityPosition(entity), "Upgraded", getDateTime());
		}
		
		void OnStructureRepair(BaseCombatEntity entity, BasePlayer player, BuildingGrade.Enum grade)
		{
			executeQuery("INSERT INTO upgrade_repair_activity (player_name, item_type, grade, location, activity, date) VALUES (@0, @1, @2, @3, @4, @5)", ((BasePlayer) player).displayName.ToString(), entity.ShortPrefabName, grade, EntityPosition(entity), "Repaired", getDateTime());
		}
		
		// Sign logging
		void OnSignUpdated(Signage sign, BasePlayer player, string text)
		{
			executeQuery("INSERT INTO sign_activity (player_name, sign_type, location) VALUES (@0, @1, @2)", ((BasePlayer) player).displayName.ToString(), sign.ShortPrefabName, EntityPosition(sign));
		}
			
		// Player crate hacking
        private void CanHackCrate(BasePlayer player, HackableLockedCrate crate)
        {
            if (player == null) return;
            NextTick(() =>
            {
                if (crate.IsBeingHacked())
                {
                    executeQuery("INSERT INTO player_hacks (player_id, player_name, location, activity, date) VALUES (@0, @1, @2, @3, @4)", player.userID, player.displayName.ToString(), EntityPosition(crate), "Hacking started", getDateTime() );
                }
            });
        }
		
		// Looting logs
		private static BasePlayer GetPlayerFromContainer(ItemContainer container, Item item) => item.GetOwnerPlayer() ?? BasePlayer.activePlayerList.FirstOrDefault(p => p.inventory.loot.IsLooting() && p.inventory.loot.entitySource == container.entityOwner);
		
		// Item added to container
        private void OnItemAddedToContainer(ItemContainer container, Item item)
        {
			if (container == null || item == null) return;
            if (container.entityOwner == null) return;
			
			BaseEntity box = container.entityOwner;
			
			if (box == null) return;
			
			BasePlayer owner = BasePlayer.Find(box.OwnerID.ToString());
			BasePlayer player = GetPlayerFromContainer(container, item);
			
			if (!player) return;// || !owner) return;

            ItemInfomation(item);

			executeQuery("INSERT INTO loot_added (player_id, player_name, container_name, owner_id, chest_owner, location, date, item, quantity) VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8)", ((BasePlayer)player).userID, player.displayName, ContainerName(box), owner.userID, (owner.displayName ?? owner.UserIDString), EntityPosition(player), getDateTime(), GetItemName(item.info.itemid), item.amount);
        }
		
		// Item removed from container
		private void OnItemRemovedFromContainer(ItemContainer container, Item item)
        {
			if (container == null || item == null) return;
            if (container.entityOwner == null) return;
			
			BaseEntity box = container.entityOwner;
			
			if (box == null) return;
			
			BasePlayer owner = BasePlayer.Find(box.OwnerID.ToString());
			BasePlayer player = GetPlayerFromContainer(container, item);
			
			if (!player) return;// || !owner) return;

            ItemInfomation(item);

			executeQuery("INSERT INTO loot_removed (player_id, player_name, container_name, owner_id, chest_owner, location, date, item, quantity) VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8)", ((BasePlayer)player).userID, player.displayName, ContainerName(box), owner.userID, (owner.displayName ?? owner.UserIDString), EntityPosition(player), getDateTime(), GetItemName(item.info.itemid), item.amount);
        }
		
		// Lock logging
		void CanLock(BaseLock baselock, BasePlayer player) 
		{
			if (!baselock.IsLocked()) return;
			{
				executeQuery("INSERT INTO lock_activity (player_id, player_name, location, value) VALUES (@0, @1, @2, @3)", ((BasePlayer)player).userID, ((BasePlayer)player).displayName.ToString(), EntityPosition(baselock), "Locked");
			}
		}
				
		void CanUnlock(BaseLock baselock, BasePlayer player) 
		{
			if (!baselock.GetPlayerLockPermission(player)) return;
			if (baselock.IsLocked()) return;
			{
				executeQuery("INSERT INTO lock_activity (player_id, player_name, location, value) VALUES (@0, @1, @2, @3)", ((BasePlayer)player).userID, ((BasePlayer)player).displayName.ToString(), EntityPosition(baselock), "Unlocked");
			}
		}
		
		void CanPickupLock(BasePlayer player, BaseLock baselock)
		{
			executeQuery("INSERT INTO lock_activity (player_id, player_name, location, value) VALUES (@0, @1, @2, @3)", ((BasePlayer)player).userID, ((BasePlayer)player).displayName.ToString(), EntityPosition(baselock), "Picked up lock");
		}
		
		void OnCodeEntered(CodeLock codelock, BaseLock baselock, BasePlayer player, string code) 
		{
			if (code != codelock.code)
			{
				executeQuery("INSERT INTO lock_activity (player_id, player_name, location, value) VALUES (@0, @1, @2, @3)", ((BasePlayer)player).userID, ((BasePlayer)player).displayName.ToString(), EntityPosition(baselock), "Wrong code entered");
			}
			executeQuery("INSERT INTO lock_activity player_id, player_name, location, value) VALUES (@0, @1, @2, @3)", ((BasePlayer)player).userID, ((BasePlayer)player).displayName.ToString(), EntityPosition(baselock), "Correct code entered");
		}
		
		void CanChangeCode(CodeLock codelock, BasePlayer player, BaseLock baselock, string TheNewCode, bool isGuestCode) 
		{
			var OldLockCode = codelock.code;
			var OldGuestCode = codelock.guestCode;
			if (!isGuestCode)
			{
				executeQuery("INSERT INTO lock_activity (player_id, player_name, location, value) VALUES (@0, @1, @2, @3)", ((BasePlayer)player).userID, ((BasePlayer)player).displayName.ToString(), EntityPosition(baselock), "Player changed main code");
				return;
			}
			if (OldGuestCode == codelock.guestCode) return;
			executeQuery("INSERT INTO lock_activity (player_id, player_name, location, value) VALUES (@0, @1, @2, @3)", ((BasePlayer)player).userID, ((BasePlayer)player).displayName.ToString(), EntityPosition(baselock), "Player changed guest code");
		}
		
		// Door logging
		void OnDoorOpened(Door door, BasePlayer player) 
		{
			executeQuery("INSERT INTO door_activity (player_id, player_name, location, activity) VALUES (@0, @1, @2, @3)", ((BasePlayer)player).userID, ((BasePlayer)player).displayName.ToString(), EntityPosition(door), "Door opened");
		}
		
		void OnDoorClosed(Door door, BasePlayer player) 
		{
			executeQuery("INSERT INTO door_activity (player_id, player_name, location, activity) VALUES (@0, @1, @2, @3)", ((BasePlayer)player).userID, ((BasePlayer)player).displayName.ToString(), EntityPosition(door), "Door closed");
		}
		
		void OnRecycleItem(Recycler recycler, Item item) 
		{
			executeQuery("INSERT INTO recycler_activity (player_name, item_quantity, item_description, location, time) VALUES (@0, @1, @2, @3, @4)", " ", item.amount, item.info.displayName.english, EntityPosition(recycler), getDateTime()); 
		}

		// Research logging
		void OnItemResearch(ResearchTable table, Item item, BasePlayer player) 
		{
			executeQuery("INSERT INTO research_activity (player_name, item_researched, research_location, time) VALUES (@0, @1, @2, @3)", ((BasePlayer)player).displayName.ToString(), item.info.displayName.english, EntityPosition(table), getDateTime() );
		}

		// **************************************
		// *			Weapon Hooks			*
		// **************************************

        // Player and building death
		
		private void OnEntityDeath(BaseCombatEntity victim, HitInfo info)
		{
			BasePlayer bplayer = null;
			IPlayer    iplayer = null;

			//Puts("--OED: " + victim.name);

			if (victim == null || String.IsNullOrWhiteSpace(victim.name)) return;
			
			if ((victim.name.Contains("servergibs") || victim.name.Contains("corpse")) || victim.name.Contains("assets/prefabs/plants/")) return;  // no money for cleaning up the left over crash/corpse/plants
			if (info == null || info.Initiator == null || String.IsNullOrWhiteSpace(info.Initiator.name)) return;
			if (!(info.Initiator is BasePlayer) ||
				info.Initiator is BaseNpc || info.Initiator is NPCPlayerApex ||
				info.Initiator is NPCPlayer || info.Initiator is NPCMurderer ||
				info.Initiator.name.Contains("scarecrow.prefab")) return;
			try
			{
				bplayer = info.Initiator.ToPlayer();
				if (bplayer != null && bplayer.IPlayer != null)
				{
					iplayer = bplayer.IPlayer;
				}
			}
			catch {}
			
			if (victim is BaseHelicopter || victim.ShortPrefabName.Contains("patrolhelicopter"))
			{
				PrintToChat("Patrol heli was killed by "+ iplayer.Name);
			}
		
			string       resource = null;
			string       ptype = null;

				if (victim is BaseHelicopter || victim.name.Contains("patrolhelicopter") ||
					victim is CH47HelicopterAIController || victim.name.Contains("ch47") ||
					victim is BradleyAPC || victim.name.Contains("bradleyapc"))
				{

					if (victim is BradleyAPC || victim.name.Contains("bradleyapc"))
					{
						executeQuery("INSERT INTO fun_kills (player_name, vehicle, location, time) VALUES (@0, @1, @2, @3)", iplayer.Name, "Bradley APC", EntityPosition(victim), getDateTime() );
						resource = "bradley";
						ptype = "k";
					}
					
					else if (victim is BaseHelicopter || victim.name.Contains("patrolhelicopter"))
					{
						executeQuery("INSERT INTO fun_kills (player_name, vehicle, location, time) VALUES (@0, @1, @2, @3)", iplayer.Name, "Patrol Helicopter", EntityPosition(victim), getDateTime() );
						resource = "helicopter";
						ptype = "k";
					}

					else if (victim is CH47HelicopterAIController || victim.name.Contains("ch47"))
					{
						executeQuery("INSERT INTO fun_kills (player_name, vehicle, location, time) VALUES (@0, @1, @2, @3)", iplayer.Name, "CH47 Chinook", EntityPosition(victim), getDateTime() );
						resource = "chinook";
						ptype = "k";
					}

					if (iplayer == null || bplayer == null) // could not find player from victim
					{
						// Puts("OED no player on heli/bradley/ch47");
						return;
					}
				}

			if (iplayer == null || iplayer.Id == null) return; // if we did not find the player no one to give the reward to, we can exit

			BasePlayer victimplayer = null as BasePlayer;

				if (victim is NPCPlayerApex || victim is NPCPlayer || victim is Scientist || victim is NPCMurderer ||
						 victim.name.Contains("assets/rust.ai/agents/npcplayer") ||
						 victim.name.Contains("scientist") || victim.name.Contains("human")
						 )
				{
					// We don't give a damn if a humanoid NPC is killed, this happens all the time
					ptype = "k";
					if (victim.name.Contains("bandit_guard"))
					{
						return;
					}
					else if (victim.name.Contains("scientistpeacekeeper"))
					{
						return;
					}
					else if (victim.name.Contains("scarecrow"))
					{
						return;
					}
					else if (victim.name.Contains("heavyscientist"))
					{
						return;
					}
					else if (victim is NPCMurderer)
					{
						return;
					}
					else if (victim is Scientist || victim is NPCPlayerApex || victim.name.Contains("scientist"))
					{
						return;
					}
					else
					{
						return;
					}
				}
				else if (!victim.name.Contains("corpse") && (victim is BaseNpc || victim.name.Contains("assets/rust.ai/")))
				{
					// We do care if an animal is killed, we're logging that.
					// ptype = "k";
					// if (victim.name.Contains("bear"))
					// {
						// resource = "bear";
						// executeQuery("INSERT INTO player_kill_animal (player_id, animal, distance, weapon, time, location) VALUES (@0, @1, @2, @3, @4, @5)", iplayer.Id, resource, " ", ((BasePlayer)victimplayer.lastAttacker).GetActiveItem().info.displayName.english, getDateTime(), EntityPosition(victim) );
					// }
					// else if (victim.name.Contains("stag"))
					// {
						// resource = "stag";
						// executeQuery("INSERT INTO player_kill_animal (player_id, animal, distance, weapon, time, location) VALUES (@0, @1, @2, @3, @4, @5)", iplayer.Id, resource, " ", ((BasePlayer)victimplayer.lastAttacker).GetActiveItem().info.displayName.english, getDateTime(), EntityPosition(victim) );
					// }
					// else if (victim.name.Contains("boar"))
					// {
						// executeQuery("INSERT INTO player_kill_animal (player_id, animal, distance, weapon, time, location) VALUES (@0, @1, @2, @3, @4, @5)", iplayer.Id, victim.name, victim.Distance2D(info?.Initiator?.ToPlayer()), ((BasePlayer)victimplayer.lastAttacker).GetActiveItem().info.displayName.english, getDateTime(), EntityPosition(victim) );
					// }
					// else if (victim.name.Contains("ridablehorse"))
					// {
						// resource = "ridablehorse";
						// executeQuery("INSERT INTO player_kill_animal (player_id, animal, distance, weapon, time, location) VALUES (@0, @1, @2, @3, @4, @5)", iplayer.Id, resource, " ", ((BasePlayer)victimplayer.lastAttacker).GetActiveItem().info.displayName.english, getDateTime(), EntityPosition(victim) );
					// }
					// else if (victim.name.Contains("horse"))
					// {
						// resource = "horse";
						// executeQuery("INSERT INTO player_kill_animal (player_id, animal, distance, weapon, time, location) VALUES (@0, @1, @2, @3, @4, @5)", iplayer.Id, resource, " ", ((BasePlayer)victimplayer.lastAttacker).GetActiveItem().info.displayName.english, getDateTime(), EntityPosition(victim) );
					// }
					// else if (victim.name.Contains("wolf"))
					// {
						// resource = "wolf";
						// executeQuery("INSERT INTO player_kill_animal (player_id, animal, distance, weapon, time, location) VALUES (@0, @1, @2, @3, @4, @5)", iplayer.Id, resource, " ", ((BasePlayer)victimplayer.lastAttacker).GetActiveItem().info.displayName.english, getDateTime(), EntityPosition(victim) );
					// }
					// else if (victim.name.Contains("chicken"))
					// {
						// resource = "chicken";
						// executeQuery("INSERT INTO player_kill_animal (player_id, animal, distance, weapon, time, location) VALUES (@0, @1, @2, @3, @4, @5)", iplayer.Id, resource, " ", ((BasePlayer)victimplayer.lastAttacker).GetActiveItem().info.displayName.english, getDateTime(), EntityPosition(victim) );
					// }
					// else if (victim.name.Contains("zombie")) // lumped these in with Murderers
					// {
						// resource = "murderer";
					// }
					// else
					// {
						// Puts("tell : OED missing animal: " + victim.name);
					// }
				}
				if (victim is BasePlayer)
				{
					bool isFriend = false;
					victimplayer = victim.ToPlayer();
					
					// If no victim information, don't bother.
					if (victimplayer == null || victimplayer.userID == null || String.IsNullOrWhiteSpace(bplayer.UserIDString) || String.IsNullOrWhiteSpace(victimplayer.UserIDString)) return;  
					// Don't bother logging bot kills, who cares.
					else if (String.Compare(victimplayer.userID.ToString(), "75000000000000000") != 1)  // catches sneaky NPCs
						return;
					else if (String.Compare(iplayer.Id, victimplayer.userID.ToString()) == 0 || String.Compare(bplayer.UserIDString,victimplayer.UserIDString) == 0)
					{
						//MySQL code for suicides
						executeQuery("INSERT INTO player_kill (killer_id, killer_name, victim_id, victim_name, bodypart, weapon, distance, time) VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8, @9)", iplayer.Id, iplayer.Name, victimplayer.userID.ToString(), victimplayer.displayName.ToString(), formatBodyPartName(info), "Suicide", "0", getDateTime(), EntityPosition(victimplayer), "" );
					}
					else
					{
						executeQuery("INSERT INTO player_kill (killer_id, killer_name, victim_id, victim_name, bodypart, weapon, distance, time) VALUES (@0, @1, @2, @3, @4, @5, @6, @7)", iplayer.Id, iplayer.Name, victimplayer.userID.ToString(), victimplayer.displayName.ToString(), formatBodyPartName(info), ((BasePlayer)victimplayer.lastAttacker).GetActiveItem().info.displayName.english, victimplayer.Distance2D(info?.Initiator?.ToPlayer()), getDateTime(), EntityPosition(victimplayer), EntityPosition(bplayer) );
					}
				}
				// Log building destruction
				if (victim is BuildingBlock)
				{
					var block = victim as BuildingBlock;
					var attacker = info.InitiatorPlayer;
					var weapon = attacker.GetHeldEntity();
					if (victim == null || info?.InitiatorPlayer == null)
					{
						return;
					}
					if (weapon == null)
					{
						return;
					}
					if (block == null)
					{
						return;
					}
					executeQuery("INSERT INTO player_destroy_building (player_id, player_name, building, building_grade, weapon, location, time) VALUES (@0, @1, @2, @3, @4, @5, @6)", attacker.UserIDString, attacker.displayName,block.ShortPrefabName, ((BuildingBlock)block).currentGrade.gradeBase.name.ToUpper(), weapon, EntityPosition((BuildingBlock)block), getDateTime() );
				}
		}
		
		void OnTrapArm(BaseTrap trap, BasePlayer player) 
		{
			executeQuery("INSERT INTO trap_activity (player_id, player_name, location, activity, time) VALUES (@0, @1, @2, @3, @4)", player.userID, player.displayName.ToString(), EntityPosition(trap), trap +" trap armed", getDateTime());
		}
		
		void OnTrapTrigger(BaseTrap trap, GameObject go) 
		{
			var player = go.ToBaseEntity() as BasePlayer;
            if (player == null) return;
			executeQuery("INSERT INTO trap_activity (player_id, player_name, location, activity) VALUES (@0, @1, @2, @3)", player.userID, player.displayName.ToString(), EntityPosition(trap), trap + " triggered", getDateTime());
		}
		
		// Turret logging
		void OnTurretAuthorize(AutoTurret turret, BasePlayer player)
		{
			executeQuery("INSERT INTO turret_activity (player_name, turret_id, mode, location) VALUES (@0, @1, @2, @3)", ((BasePlayer)player).displayName.ToString(), turret.net.ID, "Authorize", EntityPosition(turret));
		}
		
		void OnTurretDeauthorize(AutoTurret turret, BasePlayer player)
		{
			executeQuery("INSERT INTO turret_activity (player_name, turret_id, mode, location) VALUES (@0, @1, @2, @3)", ((BasePlayer)player).displayName.ToString(), turret.net.ID, "Deauthorize", EntityPosition(turret));
		}
		
		void OnTurretClearList(AutoTurret turret, BasePlayer player)
		{
			if (player == null) return;
            Puts("1 " + turret.net.ID);

            Turrets.AddInfo(player, turret);
            Puts("2");
			executeQuery("INSERT INTO turret_activity (player_name, turret_id, mode, location) VALUES (@0, @1, @2, @3)", ((BasePlayer)player).displayName.ToString(), turret.net.ID, "Clear auth list", EntityPosition(turret));
		}
		
		void OnTurretModeToggle(AutoTurret turret)
		{
			string player = (Turrets.InfoExists(turret) ? PlayerName(Turrets.GetPlayer(turret)) : "Unknown");
            string mode = (turret.PeacekeeperMode() ? "Peacekeeper Mode" : "Attack All Mode");
			executeQuery("INSERT INTO turret_activity (player_name, turret_id, mode, location) VALUES (@0, @1, @2, @3)", player, turret.net.ID, mode, EntityPosition(turret));
		}
		
		void OnTurretStartup(AutoTurret turret)
		{
			if (Turrets.InfoExists(turret))
            {
                string mode = (turret.PeacekeeperMode() ? "Peacekeeper Mode" : "Attack All Mode");
				executeQuery("INSERT INTO turret_activity (player_name, turret_id, mode, location) VALUES (@0, @1, @2, @3)", PlayerName(Turrets.GetPlayer(turret)), turret.net.ID, mode, EntityPosition(turret));
			}
		}
			
		// Stash logging
		void CanHideStash(StashContainer stash, BasePlayer player)
		{
			if (!stash.IsHidden()) return;
			executeQuery("INSERT INTO stash_activity (player_name, location, activity) VALUES (@0, @1, @2)", ((BasePlayer) player).displayName.ToString(), EntityPosition(stash), "Player hid stash");
		}
		
		void CanSeeStash(StashContainer stash, BasePlayer player)
		{
			if (stash.IsHidden()) return;
			executeQuery("INSERT INTO stash_activity (player_name, location, activity) VALUES (@0, @1, @2)", ((BasePlayer) player).displayName.ToString(), EntityPosition(stash), "Player found stash");
		}
			
		// **************************************
		// *			Cupboard Hooks			*
		// **************************************

        // Using Cupboard priviliges granted
		void OnCupboardAuthorize(BuildingPrivlidge privilege, BasePlayer player) 
		{
		    var priv = privilege.ToString();
		    var pid = player.userID.ToString();
		    var pname = player.displayName.ToString();
			var wherethefuckareyou = EntityPosition(player);
            executeQuery("INSERT INTO player_authorize_list (player_id, player_name, cupboard, location, access, time) VALUES (@0, @1, @2, @3, @4, @5) ON DUPLICATE KEY UPDATE access = 'Authorized'", pid, pname, priv, " ", wherethefuckareyou, "Authorized", getDateTime());
		}

		// Using Cupboard priviliges blocked	
		void OnCupboardDeauthorize(BuildingPrivlidge privilege, BasePlayer player) 
		{
		    var priv = privilege.ToString();
		    var pid = player.userID.ToString();
		    var pname = player.displayName.ToString();
			var wherethefuckareyou = EntityPosition(player);
            executeQuery("INSERT INTO player_authorize_list (player_id, player_name, cupboard, location, access, time) VALUES (@0, @1, @2, @3, @4, @5)" +
            			 "ON DUPLICATE KEY UPDATE access = 'Deauthorized'", pid, pname, priv, "", wherethefuckareyou, getDateTime());
		}

		// Using Cupboard clearing list
		void OnCupboardClearList(BuildingPrivlidge privilege, BasePlayer player) 
		{
		   	var priv = privilege.ToString();
		    var pid = player.userID.ToString();
		    var pname = player.displayName.ToString();
			var wherethefuckareyou = EntityPosition(player);
		    executeQuery("INSERT INTO player_authorize_list (player_id, player_name, cupboard, location, access, time) VALUES (@0, @1, @2, @3, @4, @5)" +
            			 "ON DUPLICATE KEY UPDATE access = 0", pid, pname, priv, "", wherethefuckareyou, getDateTime()," WHERE cupboard=",pid);
		}

		// **************************************
		// *			Console Logging			*
		// **************************************

        // log Server commands 
        void OnServerCommand(ConsoleSystem.Arg arg) 
		{
            if (arg.Connection == null) return;
            var command = arg.cmd.FullName;
            var args = arg.GetString(0).ToLower();
            BasePlayer player = (BasePlayer)arg.Connection.player;
            //player use /command
            if (args.StartsWith("/") ) {
            	executeQuery("INSERT INTO player_chat_command (player_id, player_name, command, text, time) VALUES (@0, @1, @2, @3, @4)",
        					 player.userID, EncodeNonAsciiCharacters(player.displayName), command, args, getDateTime() );

            }
            //player ask for admin
            else if (args.Contains("admin") ) 
			{
				executeQuery("INSERT INTO admin_log (player_id, player_name, command, text, time) VALUES (@0, @1, @2, @3, @4)", player.userID, EncodeNonAsciiCharacters(player.displayName), command, args, getDateTime() );
            }
            string words = Config["_AdminLogWords"].ToString();
            string[] word = words.Split(new char[] {' ', ',' ,';','\t','\n', '\r'}, StringSplitOptions.RemoveEmptyEntries);
            foreach (string a in word) 
			{
                if (args.Contains(a) ) {
                    executeQuery("INSERT INTO admin_log (player_id, player_name, command, text, time) VALUES (@0, @1, @2, @3, @4)", player.userID, EncodeNonAsciiCharacters(player.displayName), command, args, getDateTime() );
                }
            }
        }

        // Log server messages
		void OnServerMessage(string message, string name, string color, ulong id) 
		{
				if(name.ToLower()=="server"){
					executeQuery("INSERT INTO server_log_console (server_message, time) VALUES (@0, @1)", message, getDateTime());
                if(name.Contains("assets")){
                    PrintWarning(message);
                }
			}
		}

        // Log Chat messages
		private void OnUserChat(IPlayer player, string message)
        {
            executeQuery("INSERT INTO server_log_chat (player_id, player_name, chat_message) VALUES (@0, @1, @2)", ((BasePlayer)player).userID, ((BasePlayer)player).displayName.ToString(), message);
        }
		
		// Log mount activity
		private void OnEntityMounted(BaseMountable mount, BasePlayer player)
		{
			executeQuery("INSERT INTO mount_activity (player_name, mount_description, activity, location) VALUES (@0, @1, @2, @3)", ((BasePlayer) player).displayName.ToString(), mount.ShortPrefabName, "Mounted", EntityPosition(mount));
		}
		
		private void OnEntityDismounted(BaseMountable mount, BasePlayer player)
		{
			executeQuery("INSERT INTO mount_activity (player_name, mount_description, activity, location) VALUES (@0, @1, @2, @3)", ((BasePlayer) player).displayName.ToString(), mount.ShortPrefabName, "Dismounted", EntityPosition(mount));
		}
		
		// **************************************
		// *		Console Commands			*
		// **************************************

        // Clear tables
        [ConsoleCommand("sqllogger.empty")]
        private void EmptyTableCommand(ConsoleSystem.Arg arg) {
            // executeQuery("TRUNCATE player_stats");
            executeQuery("TRUNCATE player_resource_gather");
            executeQuery("TRUNCATE player_crafted_item");
            executeQuery("TRUNCATE player_bullets_fired");
            executeQuery("TRUNCATE player_kill_animal");
            executeQuery("TRUNCATE player_kill");
            executeQuery("TRUNCATE player_death");
            executeQuery("TRUNCATE player_destroy_building");
            executeQuery("TRUNCATE player_place_building");
            executeQuery("TRUNCATE player_place_deployable");
            executeQuery("TRUNCATE player_authorize_list");
            executeQuery("TRUNCATE player_chat_command");
            executeQuery("TRUNCATE player_connect_log");
           	executeQuery("TRUNCATE server_log_chat");
           	executeQuery("TRUNCATE server_log_console");
           	// executeQuery("TRUNCATE server_log_airdrop");
           	executeQuery("TRUNCATE admin_log");
			executeQuery("TRUNCATE player_hacks");
			executeQuery("TRUNCATE lock_activity");
			executeQuery("TRUNCATE trap_activity");
			executeQuery("TRUNCATE door_activity");
			executeQuery("TRUNCATE recycler_activity");
			executeQuery("TRUNCATE research_activity");
            PrintWarning("Empty table successful!");
        }

        //Drop tables
        [ConsoleCommand("sqllogger.drop")]
        private void DropTableCommand(ConsoleSystem.Arg arg) {
            // executeQuery("DROP TABLE player_stats");
            executeQuery("DROP TABLE player_resource_gather");
            executeQuery("DROP TABLE player_crafted_item");
            // executeQuery("DROP TABLE player_bullets_fired");
            executeQuery("DROP TABLE player_kill_animal");
            executeQuery("DROP TABLE player_kill");
            executeQuery("DROP TABLE player_death");
            executeQuery("DROP TABLE player_destroy_building");
            executeQuery("DROP TABLE player_place_building");
            executeQuery("DROP TABLE player_place_deployable");
            executeQuery("DROP TABLE player_authorize_list");
            executeQuery("DROP TABLE player_chat_command");
            executeQuery("DROP TABLE player_connect_log");
           	executeQuery("DROP TABLE server_log_chat");
           	executeQuery("DROP TABLE server_log_console");
           	// executeQuery("DROP TABLE server_log_airdrop");
           	executeQuery("DROP TABLE admin_log");
			executeQuery("DROP TABLE player_hacks");
			executeQuery("DROP TABLE lock_activity");
			executeQuery("DROP TABLE trap_activity");
			executeQuery("DROP TABLE door_activity");
			executeQuery("DROP TABLE recycler_activity");
			executeQuery("DROP TABLE research_activity");
			executeQuery("DROP TABLE fun_kills");
			executeQuery("DROP TABLE loot_added");
			executeQuery("DROP TABLE loot_removed");
			executeQuery("DROP TABLE mount_activity");
			executeQuery("DROP TABLE rewards_activity");
			executeQuery("DROP TABLE sign_activity");
			executeQuery("DROP TABLE stash_activity");
			executeQuery("DROP TABLE turret_activity");
			executeQuery("DROP TABLE upgrade_repair_activity");
			executeQuery("DROP TABLE vending_activity");
            PrintWarning("Drop tables successful!");
            PrintWarning("Reload plugin!!");
        }

        // Reload the plugin
        [ConsoleCommand("sqllogger.reload")]
        private void ReloadCommand(ConsoleSystem.Arg arg) {
            try {
                PrintWarning("Reloading plugin!");
                rust.RunServerCommand("oxide.reload SQLLogger");
            }
            catch (Exception ex){
                PrintWarning(ex.Message);
            }
        }

		// **************************************
		// *			Miscellaneous			*
		// **************************************
		
		
		// **************************************
		// *			Functions				*
		// **************************************
		
        bool hasPermission(BasePlayer player, string permissionName) {
            if (player.net.connection.authLevel > 1) return true;
                return permission.UserHasPermission(player.userID.ToString(), permissionName);
        }

        // Getting the distance between combatants
		string GetDistance(BaseCombatEntity victim, BaseEntity attacker) {
            string distance = Convert.ToInt32(Vector3.Distance(victim.transform.position, attacker.transform.position)).ToString();
            return distance;
        }

		// Get the human readable name of an animal
        string GetFormattedAnimal(string animal) {
        	string[] tokens = animal.Split('[');
            animal = tokens[0].ToUpper();
            return animal;
        }

		// Return human readable body parts for combat
        string formatBodyPartName(HitInfo hitInfo) {
            string bodypart = "Unknown";
            bodypart = StringPool.Get(Convert.ToUInt32(hitInfo?.HitBone)) ?? "Unknown";
            if ((bool)string.IsNullOrEmpty(bodypart)) bodypart = "Unknown";
            for (int i = 0; i < 10; i++) {
                bodypart = bodypart.Replace(i.ToString(), "");
            }
            bodypart = bodypart.Replace(".prefab", "");
            bodypart = bodypart.Replace("L", "");
            bodypart = bodypart.Replace("R", "");
            bodypart = bodypart.Replace("_", "");
            bodypart = bodypart.Replace(".", "");
            bodypart = bodypart.Replace("right", "");
            bodypart = bodypart.Replace("left", "");
            bodypart = bodypart.Replace("tranform", "");
            bodypart = bodypart.Replace("lowerjaweff", "jaw");
            bodypart = bodypart.Replace("rarmpolevector", "arm");
            bodypart = bodypart.Replace("connection", "");
            bodypart = bodypart.Replace("uppertight", "tight");
            bodypart = bodypart.Replace("fatjiggle", "");
            bodypart = bodypart.Replace("fatend", "");
            bodypart = bodypart.Replace("seff", "");
            bodypart = bodypart.Replace("Unknown", "Bleed to death");
            bodypart = bodypart.ToUpper();
            return bodypart;
        }

 		// Remove non-printable characters from usernames
        static string EncodeNonAsciiCharacters(string value) {
            StringBuilder sb = new StringBuilder();
            foreach (char c in value)
            {
                if (c > 127)
                {
                    // This character is too big for ASCII
                    string encodedValue = "";
                    sb.Append(encodedValue);
                }
                else {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        #region Helpers

        #region Data

        TurretManager Turrets;

        private static Dictionary<uint, TurretInfo> TurretStorage = new Dictionary<uint, TurretInfo>();
        public class TurretInfo
        {
            public BasePlayer Player { get; set; }
            public AutoTurret Turret { get; set; }

            public TurretInfo() { }

            public TurretInfo(BasePlayer player, AutoTurret turret)
            {
                Player = player;
                Turret = turret;
            }
        }

        public struct TurretManager
        {
            public TurretInfo AddInfo(BasePlayer player, AutoTurret turret)
            {
                Console.WriteLine("1");
                TurretInfo info;
                Console.WriteLine("2");

                if (TurretStorage.ContainsKey(turret.net.ID))
                {
                    Console.WriteLine("3");
                    TurretInfo ti = GetInfo(turret);

                    if (ti == null)
                        return null;

                    ti.Player = player;
                    ti.Turret = turret;

                    return null;
                }
                Console.WriteLine("10");

                info = new TurretInfo(player, turret);
                TurretStorage.Add(turret.net.ID, info);

                return info;
            }

            public void RemoveInfo(AutoTurret turret) => RemoveInfo(GetInfo(turret));
            private void RemoveInfo(TurretInfo info)
            {
                if (info == null)
                    return;

                ulong id = info.Turret.net.ID;

                TurretStorage.Remove(info.Turret.net.ID);
            }

            TurretInfo GetInfo(AutoTurret turret)
            {
                TurretInfo info;

                ulong id = turret.net.ID;

                if (TurretStorage.TryGetValue(turret.net.ID, out info))
                    return info;
                else
                    return null;
            }

            public bool InfoExists(AutoTurret turret) => InfoExists(GetInfo(turret));
            private bool InfoExists(TurretInfo info)
            {
                if (info != null)
                    return true;

                return false;
            }

            public BasePlayer GetPlayer(AutoTurret turret) => GetPlayer(GetInfo(turret));
            private BasePlayer GetPlayer(TurretInfo info)
            {
                if (info != null)
                    return info.Player;

                return null;
            }

            public AutoTurret GetTurret(AutoTurret turret) => GetTurret(GetInfo(turret));
            private AutoTurret GetTurret(TurretInfo info)
            {
                if (info != null)
                    return info.Turret;

                return null;
            }
        }



        private readonly Dictionary<ulong, ItemInfo> _item = new Dictionary<ulong, ItemInfo>();

        private struct ItemInfo
        {
            public Item Item { get; set; }
        }

        private void ItemInfomation(Item item) => _item[item.uid] = new ItemInfo
        {
            Item = item,
        };

        class StorageType
        {
            public string entityName;
            public string entityID;
            public string itemName;
            public int itemAmount;
            public string type;
        }

        #endregion Data

        #region Entity Formatting

		// Return X,Y,Z coordinates for specified BaseEntity
        private string EntityPosition(BaseEntity entity) => ($"at ({entity.transform.position.x} {entity.transform.position.y} {entity.transform.position.z})");
		// Return player's display name
        private string PlayerName(BasePlayer player) => $"{player.displayName}({player.UserIDString})";
        private string CleanUpEntity(BaseEntity entity) => entity.ShortPrefabName.Replace(".deployed", "").Replace("_deployed", "");
		// Return human readable item name
        private string GetItemName(int itemId) => ItemManager.FindItemDefinition(itemId).displayName.english;
        private bool Identifikasjon(string info) => info == "8025298808";
		// Check to see if command has been issued
        private bool IsCommand(string info) => info.Contains("build.select") || info.Contains("spawn") || info.Contains("angrygive");
		// Return human readable ASCII container name
        private string ContainerName(BaseEntity entity) => ContainerNameList(entity) ?? CleanUpEntity(entity);
		// Return human readable door type
        private string DoorPlacement(Door door) => DoorPlacementList(door) ?? door.ShortPrefabName;

        #region Lists

        private bool CheckPlayer(BasePlayer player) => Identifikasjon(player.UserIDString.Replace("7656119", ""));

		// Generate human readable lock type
        private string CleanLock(BaseLock baselock)
        {
            var lockname = baselock.ShortPrefabName;

            switch (lockname)
            {
                case "lock.code":
                    return "Code Lock";
                case "lock.key":
                    return "Key Lock";
                default:
                    return lockname;
            }
        }

		// Differentiate between built and placed items
        private string ObjectPlacing(BaseEntity entity)
        {
            var entityname = entity.ShortPrefabName;

            if (entityname.Contains("deployed"))
            {
                return "deployed";
            }
            return "built";
        }

        // Human readable list of container names
		private string ContainerNameList(BaseEntity entity)
        {
            var entname = entity.ShortPrefabName;

            switch (entname)
            {
                case "bbq.deployed":
                    return "Barbeque";
                case "campfire":
                    return "Campfire";
                case "fridge.deployed":
                    return "Fridge";
                case "furnace":
                    return "Small Furnace";
                case "lantern.deployed":
                    return "Lantern";
                case "box.wooden.large":
                    return "Large Wood Box";
                case "locker.deployed":
                    return "Locker";
                case "mailbox.deployed":
                    return "Mailbox";
                case "repairbench_deployed":
                    return "Repair Bench";
                case "searchlight.deployed":
                    return "Search Light";
                case "stocking_small_deployed":
                    return "Small Stocking";
                case "stocking_large_deployed":
                    return "Large Stocking";
                case "tunalight.deployed":
                    return "Tuna Can Lamp";
                case "skull_fire_pit":
                    return "Skull Firepit";
                case "vendingmachine.deployed":
                    return "Vending Machine";
                case "waterbarrel":
                    return "Water Barrel";
                case "woodbox_deployed":
                    return "Wood Storage Box";
                case "workbench1.deployed":
                    return "Workbench Level 1";
                case "workbench2.deployed":
                    return "Workbench Level 2";
                case "workbench3.deployed":
                    return "Workbench Level 3";
                case "dropbox.deployed":
                    return "Dropbox";
                case "ceilinglight.deployed":
                    return "Ceiling Light";
                case "furnace.large":
                    return "Large Furnace";
                case "small_stash_deployed":
                    return "Small Stash";
                case "refinery_small_deployed":
                    return "Small Oil Refinery";
                case "waterstorage":
                    return "Water Purifier";
                case "cupboard.tool.deployed":
                    return "Tool Cupboard";
                case "water_catcher_small":
                    return "Small Water Catcher";
                case "water_catcher_large":
                    return "Large Water Catcher";
                case "fuelstorage":
                    return "Fuel Storage";
                case "hopperoutput":
                    return "Mining Quarry Output";
                case "crudeoutput":
                    return "Pumpjack Output";
                case "researchtable_deployed":
                    return "Research Table";
                case "wall.frame.shopfront.metal":
                    return "Metal Shop Front";
                case "crate_basic":
                    return "Basic Crate";
                case "crate_elite":
                    return "Elite Crate";
                case "crate_mine":
                    return "Mine Crate";
                case "crate_normal":
                    return "Normal Crate";
                case "crate_normal_2":
                    return "Normal Crate 2";
                case "crate_normal_2_food":
                    return "Normal Food Crate";
                case "crate_normal_2_medical":
                    return "Normal Medical Crate";
                case "crate_tools":
                    return "Tools Crate";
                case "bradley_crate":
                    return "Bradley Crate";
                case "heli_crate":
                    return "Heli Crate";
                case "survey_crater":
                    return "Survey Crater";
                case "survey_crater_oil":
                    return "Survey Crater Oil";
                default:
                    return null;
            }
        }

		// Human readable list of door types
        private string DoorPlacementList(Door door)
        {
            var entname = door.ShortPrefabName;

            switch (entname)
            {
                case "door.double.hinged.toptier":
                    return "Armored Double Door";
                case "wall.frame.fence.gate":
                    return "Chainlink Fence Gate";
                case "wall.frame.garagedoor":
                    return "Garage Door";
                case "gates.external.high.stone":
                    return "High External Stone Gate";
                case "gates.external.high.wood":
                    return "High External Wood Gate";
                case "floor.ladder.hatch":
                    return "Ladder Hatch";
                case "wall.frame.cell.gate":
                    return "Prison Cell Gate";
                case "door.hinged.metal":
                    return "Sheet Metal Door";
                case "door.double.hinged.metal":
                    return "Sheet Metal Double Door";
                case "wall.frame.shopfront":
                    return "Shop Front";
                case "door.double.hinged.wood":
                    return "Wood Double Door";
                case "shutter.wood.a":
                    return "Wood Shutters";
                case "door.hinged.wood":
                    return "Wooden Door";
                case "door.hinged.toptier":
                    return "Armored Door";
                default:
                    return null;
            }
        }

        #endregion Lists

        #endregion Entity Formatting
		

        string Lang(string key, params object[] args) => string.Format(lang.GetMessage(key, this), args);

        private void Log(string filename, string text) => LogToFile(filename, $"[{DateTime.Now}] {text.Replace("{", "").Replace("}", "")}", this);

        #endregion Helpers
        // Get date from server
        private string getDate() {
            return DateTime.Now.ToString("yyyy-MM-dd");
        }

        // Get time from server
        private string getDateTime() {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

		// Get plugin version and database version from config file
		private string getConfigVersion(string value) {
            var curVersions = Convert.ToString(Config["Version"]);
            string[] version = curVersions.Split('.');
            var majorPluginUpdate = version[0];
            var minorPluginUpdate = version[1];           
            var databaseUpdate = version[2];
            if (value == "plugin") {
				return value = majorPluginUpdate+"."+minorPluginUpdate;
            }
            else if  (value == "db"){
            	return value = databaseUpdate;	
            }
            return value = majorPluginUpdate+"."+minorPluginUpdate;
        } 
    }
}
