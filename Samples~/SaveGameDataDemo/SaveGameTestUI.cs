using Firebase.Auth;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Suhdo.FSM.SaveGame;

namespace Suhdo.FSM.Sample.SaveGame
{
    // LỚP DỮ LIỆU MẪU: Client tự định nghĩa theo nhu cầu của Game
    [FirestoreData]
    public class SampleSaveData
    {
        [FirestoreProperty("level")] public int Level { get; set; }
        [FirestoreProperty("gold")] public long Gold { get; set; }
        [FirestoreProperty("last_save")] public Timestamp LastSave { get; set; }
        [FirestoreProperty("items")] public List<string> Items { get; set; } = new List<string>();
    }

    public class SaveGameTestUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_InputField inputLevel;
        [SerializeField] private TMP_InputField inputGold;
        [SerializeField] private TextMeshProUGUI textLog;
        
        [Header("Buttons")]
        [SerializeField] private Button btnSave;
        [SerializeField] private Button btnLoad;
        [SerializeField] private Button btnDelete;

        private ISaveGameService _saveGameService;

        private void Start()
        {
            if (FirebaseAuth.DefaultInstance.CurrentUser == null)
            {
                Log("Lỗi: Bạn cần Login để sử dụng Save Game trên Cloud!");
                return;
            }

            // Init service (Generic)
            _saveGameService = new SaveGameService(FirebaseFirestore.DefaultInstance, FirebaseAuth.DefaultInstance);

            btnSave.onClick.AddListener(() => SaveAsync());
            btnLoad.onClick.AddListener(() => LoadAsync());
            btnDelete.onClick.AddListener(() => DeleteAsync());
            
            Log("SaveGameTestUI (Generic) Ready!");
        }

        private async void SaveAsync()
        {
            Log("--- Saving (Generic) ---");
            
            var data = new SampleSaveData
            {
                Level = int.TryParse(inputLevel.text, out int lv) ? lv : 1,
                Gold = long.TryParse(inputGold.text, out long g) ? g : 0,
                LastSave = Timestamp.GetCurrentTimestamp(),
                Items = new List<string> { "Example Sword", "Generic Shield" }
            };

            // GỌI HÀM GENERIC: <SampleSaveData>
            bool success = await _saveGameService.SaveAsync<SampleSaveData>(data);
            Log(success ? $"[OK] Đã lưu thành công Snapshot của class {nameof(SampleSaveData)}" : "[LỖI] Lưu thất bại!");
        }

        private async void LoadAsync()
        {
            Log("--- Loading (Generic) ---");
            
            // TẢI VỀ KIỂU GENERIC
            var loaded = await _saveGameService.LoadAsync<SampleSaveData>();
            
            if (loaded != null)
            {
                inputLevel.text = loaded.Level.ToString();
                inputGold.text = loaded.Gold.ToString();
                Log($"[GIAI NÉN THÀNH CÔNG] Level: {loaded.Level}, Gold: {loaded.Gold}, Items: {loaded.Items.Count}");
            }
            else
            {
                Log("[THÔNG BÁO] Không có dữ liệu lưu nào của class này.");
            }
        }

        private async void DeleteAsync()
        {
            Log("--- Deleting ---");
            bool success = await _saveGameService.DeleteSaveAsync();
            Log(success ? "[OK] Đã xóa Save Cloud." : "[LỖI] Xóa thất bại!");
        }

        private void Log(string msg)
        {
            if (textLog != null)
            {
                textLog.text += $"\n{msg}";
                Debug.Log(msg);
            }
        }
    }
}
