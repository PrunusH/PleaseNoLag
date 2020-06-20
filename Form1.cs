using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sulakore.Communication;
using Sulakore.Habbo;
using Sulakore.Modules;
using Tangine;

namespace PleaseNoLag {

    [Module("PleaseNoLag", "Extensão com o propósito de reduzir o lag.")]
    [Author("0x001", HabboName = "0x001", Hotel = HHotel.ComBr, ResourceName = "", ResourceUrl = ""),
     Author("Chaos", HabboName = "!Chaos", Hotel = HHotel.ComBr, ResourceName = "", ResourceUrl = ""),
     Author("Black-Ball", HabboName = "Black-Ball", Hotel = HHotel.ComBr, ResourceName = "", ResourceUrl = ""),
     Author("Valdir.C", HabboName = "Valdir.C", Hotel = HHotel.ComBr, ResourceName = "", ResourceUrl = "")]
    public partial class Form1 : ExtensionForm {

        private Dictionary<string, int> _items;
        private Dictionary<int, bool> _keepVisibleItems;
        private Dictionary<int, int> _users, 
            _keepVisibleUsers;

        private int _index = -1;

        private bool _muteIsActivated,
            _muteChat,
            _isSelectingItemsToKeep,
            _isSelectingUsersToKeep;

        public Form1() {    
            InitializeComponent();
            this._items = new Dictionary<string, int>();
            this._keepVisibleItems = new Dictionary<int, bool>();
            this._users = new Dictionary<int, int>();
            this._keepVisibleUsers = new Dictionary<int, int>();
        }

        // Handle RoomHeightMap packet (i.e., fired when you load a room)
        [InDataCapture("RoomHeightMap")]
        private void OnNewRoomPacket(DataInterceptedEventArgs e) {
            Triggers.InDetach(In.RoomUserWhisper);

            // Reset UI and clear previously saved items (except for the ones in "keepVisible structures)

            button1.Text = "Esconder";
            button2.Text = "Ativar";

            _users.Clear();
            _items.Clear();

            // Untoggle buttons
            checkBox1.Checked = false;
            checkBox2.Checked = false;
            
            // Disable mute
            _muteIsActivated = false;

            // Reload items from _keepVisibleUsers into _users   
            foreach (var user in _keepVisibleUsers) {
                var id = user.Value;
                var index = user.Key;
                _users.Add(id, index);
            }
            
            // Get the user index using whisper method
            _index = -1;
            GetUserIndex();           
        }

        // Send whisper to himself
        private async void GetUserIndex() {
            Triggers.InAttach(In.RoomUserWhisper, OnWhisper);
            await Connection.SendToServerAsync(Out.RoomUserWhisper, "Aguarde...", 0);
        }

        // Listen for whispers and attempt to get current user virtual index from them
        private async void OnWhisper(DataInterceptedEventArgs e) {
            _index = e.Packet.ReadInteger();
            await Task.Delay(500);
            await Connection.SendToServerAsync(Out.RoomUserWhisper, "Pronto.", 0);
            Triggers.InDetach(In.RoomUserWhisper);
        }
        
        // Handle double click on furnis and add them into _keepVisibleItems if _isRunning is true (i.e. antilag is running)
        [OutDataCapture("ToggleFloorItem")]
        private void OnFurniDoubleClicked(DataInterceptedEventArgs e) {
            if (_isSelectingItemsToKeep) {
                try {
                    var furniId = e.Packet.ReadInteger().ToString();
                    if (!_keepVisibleItems.ContainsKey(_items[furniId])) {
                        _keepVisibleItems.Add(_items[furniId], true);
                    } else {
                        _keepVisibleItems[_items[furniId]] = true;
                    }
                    
                } catch (Exception _) { }
            }
        }

        // Handle double click on users and add them into _keepVisibleUsers if _isRunning is true (i.e. antilag is running)
        [OutDataCapture("RequestProfileFriends")] 
        private async void OnUserDoubleClicked(DataInterceptedEventArgs e) {
            if (_isSelectingUsersToKeep) {
                try {
                    var userId = e.Packet.ReadInteger();
                    if (!_keepVisibleUsers.ContainsKey(_users[userId])) {
                        _keepVisibleUsers.Add(_users[userId], userId);
                    } else {
                        _keepVisibleUsers[_users[userId]] = userId;
                    }

                } catch (Exception _) {}
            }
        }

        // Handle UserTalk packets. Block them if the conditions are met
        [InDataCapture("RoomUserTalk")]
        private void OnUserTalk(DataInterceptedEventArgs e) {
            var index = e.Packet.ReadInteger();
            if ((_muteIsActivated || _muteChat) && index != _index && !_keepVisibleUsers.ContainsKey(index)) {
                e.IsBlocked = true;
            }
        }

        // Handle BubbleAlert. Block them if the conditions are met
        [InDataCapture("BubbleAlert")]
        private void OnBubbleAlert(DataInterceptedEventArgs e) {
            if (_muteIsActivated || _muteChat) {
                e.IsBlocked = true;
            }            
        }

        /* I think that what the following handlers are doing is pretty obvious from now. 
            Read the above handlers if you're not sure about them. Just pay attenton to the packets names  */
        [InDataCapture("RoomUserShout")]
        private void OnUserBoldTak(DataInterceptedEventArgs e) {
            var index = e.Packet.ReadInteger();
            if ((_muteIsActivated || _muteChat) && index != _index && !_keepVisibleUsers.ContainsKey(index)) {
                e.IsBlocked = true;
            }
        }

        /* Global whisper handler. The previous one was only used to grab user id */
        private void OnUserWhisper(DataInterceptedEventArgs e) {
            var index = e.Packet.ReadInteger();
            if (!_keepVisibleUsers.ContainsKey(index) && (_muteIsActivated || _muteChat)) {
                e.IsBlocked = true;
            }
        }

        [InDataCapture("RoomUserDance")]
        private void OnUserDance(DataInterceptedEventArgs e) {
            if (_muteIsActivated) {
                e.IsBlocked = true;
            }
        }

        [InDataCapture("ReceivePrivateMessage")]
        private void OnPrivateMessage(DataInterceptedEventArgs e) {
            if ((_muteIsActivated || _muteChat)) {
                e.IsBlocked = true;
            }
        }

        [InDataCapture("RoomUserEffect")]
        private void OnUserEffect(DataInterceptedEventArgs e) {
            var index = e.Packet.ReadInteger();
            if (_muteIsActivated) {
                e.IsBlocked = true;
            }
        }

        [InDataCapture("RoomUserHandItem")]
        private void OnHandItemData(DataInterceptedEventArgs e) {
            var index = e.Packet.ReadInteger();
            if (_muteIsActivated) {
                e.IsBlocked = true;
            }
        }

        [InDataCapture("RoomUserReceivedHandItem")]
        private void OnHandItem(DataInterceptedEventArgs e) {
            var index = e.Packet.ReadInteger();
            if (_muteIsActivated) {
                e.IsBlocked = true;
            }
        }
        
        [InDataCapture("AchievementUnlocked")]
        private void OnAchievementUnlocked(DataInterceptedEventArgs e) {
            var index = e.Packet.ReadInteger();
            if (_muteIsActivated) {
                e.IsBlocked = true;
            }
        }

        [InDataCapture("RoomUserTyping")]
        private void OnUserShout(DataInterceptedEventArgs e) {
            var index = e.Packet.ReadInteger();
            if (!_keepVisibleUsers.ContainsKey(index) && (_muteIsActivated || _muteChat)) {
                e.IsBlocked = true;
            }
        }

        [InDataCapture("RoomUnitIdle")]
        private void OnUnitIdle(DataInterceptedEventArgs e) {
            var index = e.Packet.ReadInteger();
            if (_muteIsActivated) {
                e.IsBlocked = true;
            }
        }

        /* Store users data into _users. Also updates user ids that should be visible if they have re-entered the room
        and automatically remove the ones that are not in keepVisibleUsers if _isRunning is True*/ 
        [InDataCapture("RoomUsers")]
        private void OnRoomUsersPacket(DataInterceptedEventArgs e) {
            Task.Run(async () => {
                var entities = HEntity.Parse(e.Packet);
                foreach (var hEntity in entities) {
                    var flag = true;

                    // Updates user ids that should be visible if they have re-entered the room
                    if (_users.ContainsKey(hEntity.Id) && _keepVisibleUsers.ContainsKey(_users[hEntity.Id])) {
                        _keepVisibleUsers.Remove(_users[hEntity.Id]); // remove old index
                        _keepVisibleUsers.Add(hEntity.Index, hEntity.Id); // add new index
                        flag = false;   
                    }

                    _users[hEntity.Id] = hEntity.Index;
                    if (_muteIsActivated && flag) {
                        await Task.Delay(150);
                        await Connection.SendToClientAsync(In.RoomUserRemove, hEntity.Index.ToString());
                    }
                }
            });
        }     
      
        // Store floor items data into _items
        [InDataCapture("RoomFloorItems")]
        private void OnRoomFloorItems(DataInterceptedEventArgs e) {
            var floorItems = HFloorObject.Parse(e.Packet);
            for (int i = 0; i < floorItems.Length; i++) 
                _items.Add(floorItems[i].Id.ToString(), floorItems[i].TypeId);
        }

        
        // UI Handlers

        // "Remove" furnis ids stored in _keepVisibleItems from the current room. This is obviously client-side.       
        private async void Button1_Click(object sender, EventArgs e) {
            foreach(var item in _items) {
                if (!_keepVisibleItems.ContainsKey(item.Value)) {
                    await Connection.SendToClientAsync(In.RemoveFloorItem, item.Key, false, 0, 0);
                }
            }
        }

        private void Button3_Click(object sender, EventArgs e) {
            _keepVisibleItems.Clear();
        }

        private void Button4_Click(object sender, EventArgs e) {
            _keepVisibleUsers.Clear();
        }

        private void CheckBox2_CheckedChanged(object sender, EventArgs e) {
            _isSelectingUsersToKeep = true;
        }

        private void Button5_Click(object sender, EventArgs e) {
            _muteChat = !_muteChat;
            button5.Text = _muteChat ? "Desativar chat mute" : "Mutar Chat";
        }

        // "Remove" users ids stored in _keepVisibleUsers from the current room. This is obviously client-side.       
        private async void Button2_Click(object sender, EventArgs e) {
            _muteIsActivated = !_muteIsActivated;
            Triggers.InAttach(In.RoomUserWhisper, OnUserWhisper); // Attach whisper handle again
            if (_muteIsActivated) {
                foreach (var user in _users) {
                    if (!_keepVisibleUsers.ContainsKey(user.Value) && user.Value != _index) {
                        await Connection.SendToClientAsync(In.RoomUserRemove, user.Value.ToString());
                    }
                }
            }
            button2.Text = (_muteIsActivated ? "Desativar" : "Ativar");
        }

        private void CheckBox1_CheckedChanged(object sender, EventArgs e) {
            _isSelectingItemsToKeep = checkBox1.Checked;
        }
    }
}
