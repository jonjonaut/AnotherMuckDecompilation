using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatBox : MonoBehaviour
{
	private string saveDir;

	public Image overlay;

	public TMP_InputField inputField;

	public TextMeshProUGUI messages;

	public Color localPlayer;

	public Color onlinePlayer;

	public Color deadPlayer;

	private Color console = Color.cyan;

	private int maxMsgLength = 120;

	private int maxChars = 800;

	private int purgeAmount = 400;

	public static ChatBox Instance;

	public TextAsset profanityList;

	private List<string> profanity;

	public bool typing { get; set; }

	private void Awake()
	{
		Instance = this;
		HideChat();
		profanity = new List<string>();
		saveDir = Path.Combine(Application.persistentDataPath, "saves");
		string[] array = profanityList.text.Split('\n');
		foreach (string input in array)
		{
			profanity.Add(RemoveWhitespace(input));
		}
	}

	public static string RemoveWhitespace(string input)
	{
		return new string((from c in input.ToCharArray()
			where !char.IsWhiteSpace(c)
			select c).ToArray());
	}

	public void AppendMessage(int fromUser, string message, string fromUsername)
	{
		string text = TrimMessage(message);
		string text2 = "\n";
		if (fromUser != -1)
		{
			string text3 = "<color=";
			text3 = ((fromUser == LocalClient.instance.myId) ? (text3 + "#" + ColorUtility.ToHtmlStringRGB(localPlayer) + ">") : ((!GameManager.players[fromUser].dead) ? (text3 + "#" + ColorUtility.ToHtmlStringRGB(onlinePlayer) + ">") : (text3 + "#" + ColorUtility.ToHtmlStringRGB(deadPlayer) + ">")));
			text2 += text3;
		}
		if (GameManager.gameSettings.gameMode != GameSettings.GameMode.Versus || !GameManager.players[fromUser].dead || PlayerStatus.Instance.IsPlayerDead())
		{
			if (fromUser != -1 || (fromUser == -1 && fromUsername != ""))
			{
				text2 = text2 + fromUsername + ": ";
			}
			text2 += text;
			messages.text += text2;
			int length = messages.text.Length;
			if (length > maxChars)
			{
				int startIndex = length - purgeAmount;
				messages.text = messages.text.Substring(startIndex);
			}
			ShowChat();
			if (!typing)
			{
				CancelInvoke("HideChat");
				Invoke("HideChat", 5f);
			}
		}
	}

	public new void SendMessage(string message)
	{
		typing = false;
		if (message[0] == '/')
		{
			ChatCommand(message);
			return;
		}
		message = TrimMessage(message);
		if (message == "")
		{
			return;
		}
		foreach (string item in profanity)
		{
			message = Regex.Replace(message, item, "muck");
		}
		AppendMessage(0, message, GameManager.players[LocalClient.instance.myId].username);
		ClientSend.SendChatMessage(message);
		ClearMessage();
	}

	private void ClearMessage()
	{
		inputField.text = "";
		inputField.interactable = false;
	}

	private void ChatCommand(string message)
	{
		string text = message.Substring(1);
		ClearMessage();
		string text2 = "#" + ColorUtility.ToHtmlStringRGB(console);
		switch (text)
		{
		case "seed":
		{
			int seed = GameManager.gameSettings.Seed;
			AppendMessage(-1, "<color=" + text2 + ">Seed: " + seed + " (copied to clipboard)<color=white>", "");
			GUIUtility.systemCopyBuffer = string.Concat(seed);
			return;
		}
		case "ping":
			AppendMessage(-1, "<color=" + text2 + ">pong<color=white>", "");
			return;
		case "debug":
			DebugNet.Instance.ToggleConsole();
			return;
		case "kill":
			PlayerStatus.Instance.Damage(0, ignoreProtection: true);
			return;
		}
		if (text.StartsWith("give "))
		{
			text = text.Substring(5);
			int result = 1;
			int result2;
			if (text.Contains(' '))
			{
				string[] array = text.Split(' ');
				if (!int.TryParse(array[0], out result2) || !int.TryParse(array[1], out result))
				{
					return;
				}
			}
			else if (!int.TryParse(text, out result2))
			{
				return;
			}
			if (result2 >= 0 && result > 0 && result2 < ItemManager.Instance.allItems.Count)
			{
				InventoryItem inventoryItem = UnityEngine.Object.Instantiate(ItemManager.Instance.allItems[result2]);
				inventoryItem.amount = result;
				InventoryUI.Instance.AddItemToInventory(inventoryItem);
			}
			return;
		}
		if (text.StartsWith("summon "))
		{
			text = text.Substring(7);
			int result5 = 1;
			int result6;
			if (text.Contains(' '))
			{
				string[] array = text.Split(' ');
				if (!int.TryParse(array[0], out result6) || !int.TryParse(array[1], out result5))
				{
					return;
				}
			}
			else if (!int.TryParse(text, out result6))
			{
				return;
			}
			if (result6 >= 0 && result5 > 0) return;
		}
		if (text.StartsWith("help "))
        {
			text = text.Substring(5);
			int result6 = 1;
			int result7;
			string text3 = "#" + ColorUtility.ToHtmlStringRGB(console);
			if (text.Contains(' '))
			{
				string[] array2 = text.Split(' ');
				if (!int.TryParse(array2[0], out result7) || !int.TryParse(array2[1], out result6))
				{
					return;
				}
			}
			else if (!int.TryParse(text, out result7))
            {
				return;
            }
			if (result7 > 0 && result7 <= 2)
            {
				switch (result7)
                {
                    case 1:
						{
							AppendMessage(-1, "<color=" + text3 + ">/give - gives the player an item from an array to 0 - 243 " + "/powerup - gives the player a power up from 0 - " + ItemManager.Instance.allPowerups.Values + "<color=white>", "");
							return;
						}
						
                    case 2:
						{
							AppendMessage(-1, "<color=" + text3 + ">/clearpowerups - Clears all powerups " + "/clearinventory - Clears the entire Inventory<color=white>", "");
							return;
						}
				}
				if (result7 > 2 || result7 < 0)
                {
					AppendMessage(-1, "<color=yellow>" + "Available Arguments (1-2)" + "<color=white>", "");
				} 

				return;
			}
			return;
		}
		if (text.StartsWith("powerup "))
		{
			text = text.Substring(8);
			int result3 = 1;
			int result4;
			if (text.Contains(' '))
			{
				string[] array2 = text.Split(' ');
				if (!int.TryParse(array2[0], out result4) || !int.TryParse(array2[1], out result3))
				{
					return;
				}
			}
			else if (!int.TryParse(text, out result4))
			{
				return;
			}
			if (result4 >= 0 && result4 < ItemManager.Instance.allPowerups.Count)
			{
				PowerupInventory.Instance.AddPowerup(result4, result3);
			}
			return;
		}
		if (text.StartsWith("bulk "))
		{
			string[] array3 = text.Substring(5).Split(';');
			foreach (string text3 in array3)
			{
				try
				{
					ChatCommand("/" + text3);
				}
				catch (Exception message2)
				{
					Debug.LogError(message2);
				}
			}
			return;
		}
		switch (text)
		{
		case "saveinventory":
		{
			List<string> list = new List<string>();
			InventoryCell[] allCells = InventoryUI.Instance.allCells;
			foreach (InventoryCell inventoryCell2 in allCells)
			{
				if (inventoryCell2.currentItem != null)
				{
					list.Add($"give {inventoryCell2.currentItem.id} {inventoryCell2.currentItem.amount}");
				}
			}
			foreach (Powerup value in ItemManager.Instance.allPowerups.Values)
			{
				int amount = PowerupInventory.Instance.GetAmount(value.name);
				if (amount != 0)
				{
					list.Add($"powerup {value.id} {amount}");
				}
			}
			GUIUtility.systemCopyBuffer = "/bulk " + string.Join(";", list);
			AppendMessage(-1, "<color=" + text2 + ">Copied inventory command to clipboard<color=white>", "");
			break;
		}
		case "clearinventory":
		{
			InventoryCell[] allCells = InventoryUI.Instance.allCells;
			foreach (InventoryCell inventoryCell in allCells)
			{
				if (inventoryCell.currentItem != null)
				{
					inventoryCell.currentItem = null;
					inventoryCell.UpdateCell();
				}
			}
			break;
		}
		case "clearpowerups":
		{
			foreach (Powerup value2 in ItemManager.Instance.allPowerups.Values)
			{
				int amount2 = PowerupInventory.Instance.GetAmount(value2.name);
				if (amount2 != 0)
				{
					PowerupInventory.Instance.AddPowerup(value2.id, -amount2);
				}
			}
			break;
		}
		case "dontdestroyneighbors":
			if (!LocalClient.serverOwner)
			{
				AppendMessage(-1, "<color=" + text2 + ">Only the server host can enable/disable destroying neighbors<color=white>", "");
			}
			else
			{
				ServerSend.DontDestroy(!BuildDestruction.dontDestroy);
			}
			break;
		}
	}

	private string TrimMessage(string message)
	{
		if (string.IsNullOrEmpty(message))
		{
			return "";
		}
		return message.Substring(0, Mathf.Min(message.Length, maxMsgLength));
	}

	private void UserInput()
	{
		if (Input.GetKeyDown(KeyCode.Return))
		{
			if (typing)
			{
				SendMessage(inputField.text);
			}
			else
			{
				ShowChat();
				inputField.interactable = true;
				inputField.Select();
				typing = true;
			}
		}
		if (typing && !inputField.isFocused)
		{
			inputField.Select();
		}
		if (Input.GetKeyDown(KeyCode.Escape) && typing)
		{
			ClearMessage();
			typing = false;
			CancelInvoke("HideChat");
			Invoke("HideChat", 5f);
		}
	}

	private void Update()
	{
		UserInput();
	}

	private void HideChat()
	{
		if (!typing)
		{
			typing = false;
			overlay.CrossFadeAlpha(0f, 1f, ignoreTimeScale: true);
			messages.CrossFadeAlpha(0f, 1f, ignoreTimeScale: true);
			inputField.GetComponent<Image>().CrossFadeAlpha(0f, 1f, ignoreTimeScale: true);
			inputField.GetComponentInChildren<TextMeshProUGUI>().CrossFadeAlpha(0f, 1f, ignoreTimeScale: true);
		}
	}

	private void ShowChat()
	{
		overlay.CrossFadeAlpha(1f, 0.2f, ignoreTimeScale: true);
		messages.CrossFadeAlpha(1f, 0.2f, ignoreTimeScale: true);
		inputField.GetComponent<Image>().CrossFadeAlpha(0.2f, 1f, ignoreTimeScale: true);
		inputField.GetComponentInChildren<TextMeshProUGUI>().CrossFadeAlpha(0.2f, 1f, ignoreTimeScale: true);
	}
}
