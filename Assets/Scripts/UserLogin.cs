using UnityEngine;
using Firebase.Database;
using UnityEngine.UI;
using PimDeWitte.UnityMainThreadDispatcher;

public class UserLogin : MonoBehaviour
{
    FirebaseDatabase database;
    DatabaseReference reference;
    UnityMainThreadDispatcher dispatcher;

    [SerializeField] InputField NickNameInput;
    [SerializeField] Text checkText;

    void Start()
    {
        database = FirebaseDatabase.GetInstance(
            "https://finalexam-97889-default-rtdb.asia-southeast1.firebasedatabase.app/"
        );

        reference = database.RootReference;
        dispatcher = UnityMainThreadDispatcher.Instance();
    }

    public void OnClickLogin()
    {
        string nickName = NickNameInput.text.Trim();

        if (string.IsNullOrEmpty(nickName))
        {
            checkText.text = "닉네임을 입력하세요.";
            return;
        }

        checkText.text = "로그인 시도 중...";

        reference.Child("UserInfo").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                dispatcher.Enqueue(() =>
                {
                    checkText.text = "Firebase 읽기 오류";
                });
                return;
            }

            DataSnapshot snapshot = task.Result;

            bool found = false;

            foreach (DataSnapshot userSnapshot in snapshot.Children)
            {
                string dbNickName = userSnapshot.Child("NickName").Value.ToString();

                if (dbNickName == nickName)
                {
                    found = true;

                    string userKey = userSnapshot.Key;

                    dispatcher.Enqueue(() =>
                    {
                        PlayerPrefs.SetString("UserKey", userKey);
                        PlayerPrefs.SetString("UserNickName", nickName);
                        PlayerPrefs.Save();

                        checkText.text = "로그인 성공";
                    });

                    break;
                }
            }

            if (!found)
            {
                dispatcher.Enqueue(() =>
                {
                    checkText.text = "존재하지 않는 닉네임입니다.";
                });
            }
        });
    }
}