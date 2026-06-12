using UnityEngine;
using Firebase.Database;
using UnityEngine.UI;
using PimDeWitte.UnityMainThreadDispatcher;
using Newtonsoft.Json;
using System.Collections.Generic;

public class UnitShopManager : MonoBehaviour
{
    FirebaseDatabase database;
    DatabaseReference reference;
    UnityMainThreadDispatcher dispatcher;

    [Header("UI")]
    [SerializeField] Text CoinText;
    [SerializeField] Text MessageText;

    string userKey;

    int currentCoin;

    Dictionary<string, bool> unitList =
        new Dictionary<string, bool>();

    void Start()
    {
        database = FirebaseDatabase.GetInstance(
            "https://finalexam-97889-default-rtdb.asia-southeast1.firebasedatabase.app/"
        );

        reference = database.RootReference;
        dispatcher = UnityMainThreadDispatcher.Instance();

        LoadUserData();
    }

    void LoadUserData()
    {
        userKey = PlayerPrefs.GetString("UserKey");

        if (string.IsNullOrEmpty(userKey))
        {
            MessageText.text = "로그인 정보가 없습니다.";
            return;
        }

        reference.Child("UserInfo")
            .Child(userKey)
            .GetValueAsync()
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    dispatcher.Enqueue(() =>
                    {
                        MessageText.text = "유저 정보 불러오기 실패";
                    });
                    return;
                }

                DataSnapshot snapshot = task.Result;

                currentCoin =
                    int.Parse(snapshot.Child("Coin").Value.ToString());

                string unitListJson =
                    snapshot.Child("UnitList").Value.ToString();

                unitList =
                    JsonConvert.DeserializeObject<Dictionary<string, bool>>
                    (unitListJson);

                dispatcher.Enqueue(() =>
                {
                    RefreshUI();
                    MessageText.text = "유닛 데이터 로드 완료";
                });
            });
    }

    void RefreshUI()
    {
        CoinText.text = "Coin : " + currentCoin;
    }

    public void OnClickBuyUnit2()
    {
        BuyUnit("Unit2", 30);
    }

    public void OnClickBuyUnit3()
    {
        BuyUnit("Unit3", 50);
    }

    public void OnClickBuyUnit4()
    {
        BuyUnit("Unit4", 70);
    }

    void BuyUnit(string unitName, int price)
    {
        if (currentCoin < price)
        {
            MessageText.text = "코인이 부족합니다.";
            return;
        }

        if (unitList.ContainsKey(unitName) &&
            unitList[unitName])
        {
            MessageText.text =
                "이미 보유한 유닛입니다.";
            return;
        }

        currentCoin -= price;

        unitList[unitName] = true;

        SaveUnitData(unitName);
    }

    void SaveUnitData(string unitName)
    {
        string unitListJson =
            JsonConvert.SerializeObject(unitList);

        Dictionary<string, object> updateData =
            new Dictionary<string, object>();

        updateData["Coin"] = currentCoin;
        updateData["UnitList"] = unitListJson;

        reference.Child("UserInfo")
            .Child(userKey)
            .UpdateChildrenAsync(updateData)
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    dispatcher.Enqueue(() =>
                    {
                        MessageText.text =
                            "유닛 저장 실패";
                    });
                    return;
                }

                dispatcher.Enqueue(() =>
                {
                    RefreshUI();

                    MessageText.text =
                        unitName + " 구매 완료";
                });
            });
    }
}