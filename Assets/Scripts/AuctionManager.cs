using UnityEngine;
using Firebase.Database;
using UnityEngine.UI;
using PimDeWitte.UnityMainThreadDispatcher;
using Newtonsoft.Json;
using System.Collections.Generic;


public class AuctionManager : MonoBehaviour
{
    FirebaseDatabase database;
    DatabaseReference reference;
    UnityMainThreadDispatcher dispatcher;

    [Header("UI")]
    [SerializeField] Text PotionCountText;
    [SerializeField] Text BombCountText;
    [SerializeField] Text TicketCountText;
    [SerializeField] Text MessageText;

    [Header("Sell Price Input")]
    [SerializeField] InputField PriceInput;

    string userKey;
    string nickName;

    int currentCoin;

    Dictionary<string, int> inventory = new Dictionary<string, int>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        database = FirebaseDatabase.GetInstance(
            "https://finalexam-97889-default-rtdb.asia-southeast1.firebasedatabase.app/"
        );

        reference = database.RootReference;
        dispatcher = UnityMainThreadDispatcher.Instance();

        LoadMyData();
    }

    void LoadMyData()
    {
        userKey = PlayerPrefs.GetString("UserKey");

        if (string.IsNullOrEmpty(userKey))
        {
            MessageText.text = "로그인 정보가 없습니다.";
            return;
        }

        reference.Child("UserInfo").Child(userKey).Child("Inventory").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                dispatcher.Enqueue(() =>
                {
                    MessageText.text = "내 정보 불러오기 실패";
                });
                return;
            }

            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                currentCoin = int.Parse(snapshot.Child("Coin").Value.ToString());

                string inventoryJson = snapshot.Child("Inventory").Value.ToString();

                inventory = JsonConvert.DeserializeObject<Dictionary<string, int>>(inventoryJson);

                dispatcher.Enqueue(() =>
                {
                    RefreshUI();
                    MessageText.text = "인벤토리 불러오기 완료";
                });
            }
        });
    }

    int GetItemCount(string itemName)
    {
        if (inventory.ContainsKey(itemName))
        {
            return inventory[itemName];
        }
        
        return 0;
    }

    void RefreshUI()
    {
        PotionCountText.text = "Potion : " + GetItemCount("Potion");
        BombCountText.text = "Bomb : " + GetItemCount("Bomb");
        TicketCountText.text = "Ticket : " + GetItemCount("Ticket");
    }


    void SellItem(string itemName)
    {
        if (string.IsNullOrEmpty(PriceInput.text))
        {
            MessageText.text = "판매 가격을 입력하세요.";
            return;
        }

        int price = int.Parse(PriceInput.text);

        if (price <= 0)
        {
            MessageText.text = "가격은 1 이상 이어야 합니다.";
            return;
        }

        if (!inventory.ContainsKey(itemName) || inventory[itemName] <= 0)
        {
            MessageText.text = itemName + "아이템이 없습니다.";
            return;
        }

        inventory[itemName]--;

        string inventoryJson = JsonConvert.SerializeObject(inventory);

        DatabaseReference auctionRef = reference.Child("AuctionList").Push();
        string auctionKey = auctionRef.Key;

        Dictionary<string, object> updataData = new Dictionary<string, object>();

        updataData["UserInfo/" + userKey + "/Inventory"] = inventoryJson;

        updataData["AuctionList/" + auctionKey + "/AuctionKey"] = auctionKey;
        updataData["AuctionList/" + auctionKey + "/SellerKey"] = userKey;
        updataData["AuctionList/" + auctionKey + "/SellerNickName"] = nickName;
        updataData["AuctionList/" + auctionKey + "/ItemName"] = itemName;
        updataData["AuctionList/" + auctionKey + "/Count"] = 1;
        updataData["AuctionList/" + auctionKey + "/Price"] = price;
        updataData["AuctionList/" + auctionKey + "/IsSold"] = false;
        updataData["AuctionList/" + auctionKey + "/ListeAt"] = ServerValue.Timestamp;

        reference.UpdateChildrenAsync(updataData).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                dispatcher.Enqueue(() =>
                {
                    MessageText.text = "판매 등록 실패";
                });
            }
            if (task.IsCompleted)
            {
                dispatcher.Enqueue(() =>
                {
                    MessageText.text = "판매 등록 완료";
                });
            }
        });

    }


    public void OnClickSellPotion()
    {
        SellItem("Potion");
    }

    public void OnClickSellBomb()
    {
        SellItem("Bobm");
    }

    public void OnClickSellTicket()
    {
        SellItem("Ticket");
    }



}
