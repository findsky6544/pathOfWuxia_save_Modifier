using Heluo;
using Heluo.Data;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using static System.Windows.Forms.ListViewItem;
using UnityEngine;
using MoonSharp.Interpreter;
using System.Collections;
using Heluo.Manager;
using static 侠之道存档修改器.EnumData;
using Heluo.Flow;
using Heluo.Tree;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Reflection;

namespace 侠之道存档修改器
{
    public partial class Form1 : Form
    {
        AutoSizeFormClass asc = new AutoSizeFormClass();
        private string saveFilesPath = "saveFilesPath.txt";
        private string FlagRemarkFilePath = "FlagRemark.txt";
        private string logPath = "output.log";
        private GameData gameData = new GameData();
        private PathOfWuxiaSaveHeader pathOfWuxiaSaveHeader = new PathOfWuxiaSaveHeader();
        private DataManager Data;
        private bool saveFileIsSelected = false;
        private bool isSaveFileSelecting = false;

        private Dictionary<string, ComboBoxItem> dcbi = new Dictionary<string, ComboBoxItem>();

        static void SetDefaultCulture(CultureInfo culture)
        {
            Type type = typeof(CultureInfo);

            try
            {
                type.InvokeMember("s_userDefaultCulture",
                                    BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Static,
                                    null,
                                    culture,
                                    new object[] { culture });

                type.InvokeMember("s_userDefaultUICulture",
                                    BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Static,
                                    null,
                                    culture,
                                    new object[] { culture });
            }
            catch (Exception ex)
            {
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }

            try
            {
                type.InvokeMember("m_userDefaultCulture",
                                    BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Static,
                                    null,
                                    culture,
                                    new object[] { culture });

                type.InvokeMember("m_userDefaultUICulture",
                                    BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Static,
                                    null,
                                    culture,
                                    new object[] { culture });
            }
            catch (Exception ex)
            {
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        public Form1()
        {
            if (File.Exists(logPath))
            {
                StreamWriter sr = new StreamWriter(logPath);

                sr.Write("");
                sr.Close();
            }
            XMLHelper.ReadXml();

            SetDefaultCulture(CultureInfo.CreateSpecificCulture("zh-CN"));
            Data = new DataManager();
            LogHelper.Debug("initalize");
            try
            {
                InitializeComponent();

                Game.Data = Data;

                getConfigDatas();

                if (File.Exists(saveFilesPath))
                {
                    StreamReader sr = new StreamReader(saveFilesPath);
                    string line;

                    // 从文件读取并显示行，直到文件的末尾 
                    while ((line = sr.ReadLine()) != null)
                    {
                        SaveFilesPathTextBox.Text = line;
                    }

                    getSaveFiles();
                    sr.Close();
                }
            }
            catch (Exception ex)
            {
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }



        }
        //2. 为窗体添加Load事件，并在其方法Form1_Load中，调用类的初始化方法，记录窗体和其控件的初始位置和大小
        private void Form1_Load(object sender, EventArgs e)
        {
            if (!asc.isInit)
            {
                asc.controllInitializeSize(this);
            }
        }
        //3.为窗体添加SizeChanged事件，并在其方法Form1_SizeChanged中，调用类的自适应方法，完成自适应
        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            if (!asc.isInit)
            {
                asc.controllInitializeSize(this);
            }
            asc.controlAutoSize(this);
            //this.WindowState = (System.Windows.Forms.FormWindowState)(2);//记录完控件的初始位置和大小后，再最大化
        }

        public string getBaseFlowGraphStr(BaseFlowGraph bfg)
        {
            string str = "";

            if (bfg != null)
            {
                for (int i = 1; i < bfg.nodes.Count; i++)
                {
                    str += bfg.nodes[i] + ",";
                }
                str = str.Substring(0, str.Length - 1);
            }

            return str;
        }

        private void selectsaveFilesPathButton_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("selectsaveFilesPathButton_Click");
            try
            {
                FolderBrowserDialog dialog = new FolderBrowserDialog();
                dialog.Description = "请选择存档文件夹，路径为Steam\\userdata\\xxxxxxxx\\1189630\\remote";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    SaveFilesPathTextBox.Text = dialog.SelectedPath;
                    StreamWriter sw = new StreamWriter(saveFilesPath);
                    sw.WriteLine(dialog.SelectedPath);
                    sw.Close();

                    getSaveFiles();
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void getSaveFiles()
        {
            LogHelper.Debug("getSaveFiles");
            try
            {
                messageLabel.Text = "";
                DirectoryInfo folder = new DirectoryInfo(SaveFilesPathTextBox.Text);
                SaveFileListBox.Items.Clear();
                List<FileInfo> fileList = folder.GetFiles().ToList();
                fileList.Remove(fileList.Find(f => f.Name == "BS.save"));
                fileList = fileList.OrderBy(f => int.Parse(Regex.Match(f.Name, @"\d+").Value)).ToList();
                for (int i = 0; i < fileList.Count; i++)
                {
                    FileInfo file = fileList[i];
                    if (file.Name.Contains("PathOfWuxia") && file.Name.Contains("save"))
                    {
                        SaveFileListBox.Items.Add(file.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }

        }

        private void saveFileListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            LogHelper.Debug("saveFileListBox_SelectedIndexChanged");
            try
            {
                isSaveFileSelecting = true;
                string saveFilePath = SaveFilesPathTextBox.Text + "\\" + SaveFileListBox.SelectedItem.ToString();

                FileStream readstream = File.OpenRead(saveFilePath);
                StreamReader sr = new StreamReader(readstream);

                byte[] array = new byte[17];
                sr.BaseStream.Read(array, 0, array.Length);
                if (array[0] == 239 && array[1] == 187 && array[2] == 191)
                {
                    sr.BaseStream.Position = 3L;
                    sr.BaseStream.Read(array, 0, array.Length);
                }

                string @string = Encoding.ASCII.GetString(array);
                if (@string == "WUXIASCHOOL_B_1_0")
                {
                    pathOfWuxiaSaveHeader = LZ4MessagePackSerializer.Deserialize<PathOfWuxiaSaveHeader>(sr.BaseStream, HeluoResolver.Instance, true);
                }
                readstream.Close();


                readstream = File.OpenRead(saveFilePath);
                sr = new StreamReader(readstream);

                array = new byte[17];
                sr.BaseStream.Read(array, 0, array.Length);
                if (array[0] == 239 && array[1] == 187 && array[2] == 191)
                {
                    sr.BaseStream.Position = 3L;
                    sr.BaseStream.Read(array, 0, array.Length);
                }

                @string = Encoding.ASCII.GetString(array);
                if (@string == "WUXIASCHOOL_B_1_0")
                {

                    LZ4MessagePackSerializer.Deserialize<PathOfWuxiaSaveHeader>(sr.BaseStream, HeluoResolver.Instance, true);
                    gameData = LZ4MessagePackSerializer.Deserialize<GameData>(sr.BaseStream, HeluoResolver.Instance, true);

                    Game.GameData = gameData;


                    saveFileIsSelected = true;
                    readDatas();

                }
                readstream.Close();

                isSaveFileSelecting = false;

                CharacterListView.Enabled = true;
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void getConfigDatas()
        {
            LogHelper.Debug("getConfigDatas");
            try
            {
                readAllMap();
                readAllRound();
                readAllGameLevel();
                readAllCharacter();
                readAllExterior();
                readAllElement();
                readAllSpecialSkill();
                readAllEquip();
                readAllSkill();
                readAllInventory();
                readAllTrait();
                readAllMantra();
                readAllGender();
                readAllQuestState();
                readShowAllQuest();
                readAllBook();
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void readAllInventory()
        {
            LogHelper.Debug("readAllInventory");
            try
            {
                foreach (KeyValuePair<string, Props> kv in Data.Get<Props>())
                {

                    ListViewItem lvi = new ListViewItem();

                    lvi.Text = kv.Key;
                    lvi.SubItems.Add(kv.Value.Name);
                    lvi.SubItems.Add(((EnumData.PropsType)kv.Value.PropsType).ToString());
                    lvi.SubItems.Add(EnumData.GetDisplayName(kv.Value.PropsCategory));
                    lvi.SubItems.Add(kv.Value.Price.ToString());
                    lvi.SubItems.Add(EnumData.GetDisplayName(kv.Value.CanDeals));
                    lvi.SubItems.Add(EnumData.GetDisplayName(kv.Value.IsShow));
                    lvi.SubItems.Add(EnumData.GetDisplayName(kv.Value.UseTime));
                    lvi.SubItems.Add(kv.Value.PropsEffectDescription.ToString());

                    string canUseIds = "";
                    if (kv.Value.CanUseID != null)
                    {
                        for (int i = 0; i < kv.Value.CanUseID.Count; i++)
                        {
                            string canUseId = kv.Value.CanUseID[i];
                            if (canUseId == "Player")
                            {
                                canUseIds += "玩家,";
                            }
                            else
                            {
                                canUseIds += Data.Get<Npc>(canUseId).Name + ",";
                            }
                        }
                    }
                    else
                    {
                        canUseIds = ",";
                    }
                    canUseIds = canUseIds.Substring(0, canUseIds.Length - 1);
                    lvi.SubItems.Add(canUseIds);

                    PropsListView.Items.Add(lvi);
                }

                PropsListView.EndUpdate();  //结束数据处理，UI界面一次性绘制。 
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void readAllMap()
        {
            LogHelper.Debug("readAllMap");
            try
            {
                CurrentMapComboBox.DisplayMember = "value";
                CurrentMapComboBox.ValueMember = "key";
                foreach (KeyValuePair<string, Map> kv in Data.Get<Map>())
                {
                    ComboBoxItem cbi = new ComboBoxItem(kv.Value.Id, kv.Value.Name);
                    CurrentMapComboBox.Items.Add(cbi);
                    dcbi.Add(cbi.key, cbi);
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void readAllRound()
        {
            LogHelper.Debug("readAllRound");
            try
            {
                for (int i = 1; i <= 3; i++)
                {
                    CurrentYearComboBox.Items.Add((EnumData.Year)i);
                }
                for (int i = 1; i <= 12; i++)
                {
                    CurrentMonthComboBox.Items.Add((EnumData.Month)i);
                }
                for (int i = 1; i <= 5; i++)
                {
                    CurrentRoundOfMonthComboBox.Items.Add((EnumData.RoundOfMonth)i);
                }
                for (int i = 1; i <= 2; i++)
                {
                    CurrentTimeComboBox.Items.Add((EnumData.Time)i);
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void readAllGameLevel()
        {
            LogHelper.Debug("readAllGameLevel");
            try
            {
                for (int i = 1; i <= 4; i++)
                {
                    GameLevelComboBox.Items.Add((EnumData.GameLevel)i);
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void readAllCharacter()
        {
            LogHelper.Debug("readAllCharacter");
            try
            {
                foreach (KeyValuePair<string, CharacterInfo> kv in Data.Get<CharacterInfo>())
                {

                    ListViewItem lvi = new ListViewItem();

                    lvi.Text = kv.Key;
                    if (lvi.Text == "in0101")
                    {
                        lvi.Text = "Player";
                    }
                    lvi.SubItems.Add(kv.Value.Remark);

                    CharacterListView.Items.Add(lvi);
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void readAllExterior()
        {
            LogHelper.Debug("readAllExterior");
            try
            {
                foreach (KeyValuePair<string, CharacterExterior> kv in Data.Get<CharacterExterior>())
                {

                    ListViewItem lvi = new ListViewItem();

                    lvi.Text = kv.Key;
                    if (lvi.Text == "in0101")
                    {
                        lvi.Text = "Player";
                    }
                    lvi.SubItems.Add("");
                    lvi.SubItems.Add(kv.Value.Remark);

                    CharacterExteriorListView.Items.Add(lvi);
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void readAllSkill()
        {
            LogHelper.Debug("readAllSkill");
            try
            {
                SkillListView.Items.Clear();
                foreach (KeyValuePair<string, Skill> kv in Data.Get<Skill>())
                {
                    if (WeaponComboBox.SelectedIndex != -1)
                    {
                        string weaponId = ((ComboBoxItem)WeaponComboBox.SelectedItem).key;
                        Props prop = Data.Get<Props>(weaponId);

                        if (kv.Value.Type != prop.PropsCategory && kv.Value.Type != Heluo.Data.PropsCategory.Throwing && kv.Value.DamageType != DamageType.Heal && kv.Value.DamageType != DamageType.Summon)
                        {
                            continue;
                        }
                    }

                    ListViewItem lvi = new ListViewItem();

                    lvi.Text = kv.Key;
                    lvi.SubItems.Add(kv.Value.Name);
                    lvi.SubItems.Add("");
                    lvi.SubItems.Add(EnumData.GetDisplayName(kv.Value.RequireAttribute));
                    lvi.SubItems.Add(kv.Value.RequireValue.ToString());
                    lvi.SubItems.Add(EnumData.GetDisplayName(kv.Value.Type));
                    lvi.SubItems.Add(EnumData.GetDisplayName(kv.Value.DamageType));
                    lvi.SubItems.Add(EnumData.GetDisplayName(kv.Value.TargetType));
                    lvi.SubItems.Add(EnumData.GetDisplayName(kv.Value.TargetArea));
                    lvi.SubItems.Add(kv.Value.MinRange + "-" + kv.Value.MaxRange);
                    lvi.SubItems.Add(kv.Value.AOE.ToString());
                    lvi.SubItems.Add(kv.Value.RequestMP.ToString());
                    lvi.SubItems.Add(kv.Value.MaxCD.ToString());
                    lvi.SubItems.Add(kv.Value.PushDistance == -1 ? "抓取" : kv.Value.PushDistance.ToString());
                    if (kv.Value.Summonid != "0" && !string.IsNullOrEmpty(kv.Value.Summonid))
                    {
                        string[] summonids = kv.Value.Summonid.Split(',');
                        string summonName = "";
                        for (int i = 0; i < summonids.Length; i++)
                        {
                            summonName += Data.Get<Npc>(summonids[i]).Name + ",";
                        }
                        summonName = summonName.Substring(0, summonName.Length - 1);
                        lvi.SubItems.Add(summonName);
                    }
                    else
                    {
                        lvi.SubItems.Add("");
                    }

                    lvi.SubItems.Add(kv.Value.Description.ToString());

                    SkillListView.Items.Add(lvi);
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void readExteriorName()
        {
            LogHelper.Debug("readExteriorName");
            try
            {
                if (!saveFileIsSelected)
                {
                    string message = "请先选择一个存档";
                    messageLabel.Text = message;
                    LogHelper.Debug(message);
                    return;
                }
                foreach (ListViewItem lvi in CharacterExteriorListView.Items)
                {
                    lvi.SubItems[1].Text = Game.GameData.Exterior[lvi.Text].FullName();
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void readAllElement()
        {
            LogHelper.Debug("readAllElement");
            try
            {
                for (int i = 0; i <= 5; i++)
                {
                    ElementComboBox.Items.Add(EnumData.GetDisplayName((Element)i));
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void readAllSpecialSkill()
        {
            LogHelper.Debug("readAllSpecialSkill");
            try
            {
                SpecialSkillComboBox.DisplayMember = "value";
                SpecialSkillComboBox.ValueMember = "key";
                foreach (KeyValuePair<string, Skill> skill in Data.Get<Skill>())
                {
                    if (skill.Value.Id.Contains("specialskill"))
                    {
                        ComboBoxItem cbi = new ComboBoxItem(skill.Value.Id, skill.Value.Name);
                        SpecialSkillComboBox.Items.Add(cbi);
                        dcbi.Add(cbi.key, cbi);
                    }
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void readAllEquip()
        {
            LogHelper.Debug("readAllEquip");
            try
            {
                WeaponComboBox.DisplayMember = "value";
                WeaponComboBox.ValueMember = "key";
                WeaponComboBox2.DisplayMember = "value";
                WeaponComboBox2.ValueMember = "key";

                ClothComboBox.DisplayMember = "value";
                ClothComboBox.ValueMember = "key";

                JewelryComboBox.DisplayMember = "value";
                JewelryComboBox.ValueMember = "key";
                foreach (KeyValuePair<string, Props> props in Data.Get<Props>())
                {
                    ComboBoxItem cbi = new ComboBoxItem(props.Value.Id, props.Value.Name + "-" + props.Value.PropsEffectDescription);
                    if (props.Value.PropsType == Heluo.Data.PropsType.Weapon)
                    {
                        WeaponComboBox.Items.Add(cbi);
                        WeaponComboBox2.Items.Add(cbi);
                        dcbi.Add(cbi.key, cbi);
                    }
                    else if (props.Value.PropsType == Heluo.Data.PropsType.Armor)
                    {
                        ClothComboBox.Items.Add(cbi);
                        dcbi.Add(cbi.key, cbi);
                    }
                    else if (props.Value.PropsType == Heluo.Data.PropsType.Accessories)
                    {
                        JewelryComboBox.Items.Add(cbi);
                        dcbi.Add(cbi.key, cbi);
                    }
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void readAllTrait()
        {
            LogHelper.Debug("readAllTrait");
            try
            {
                foreach (KeyValuePair<string, Trait> kv in Data.Get<Trait>())
                {

                    ListViewItem lvi = new ListViewItem();

                    lvi.Text = kv.Key;
                    lvi.SubItems.Add(kv.Value.Name);
                    lvi.SubItems.Add(kv.Value.Description);

                    TraitListView.Items.Add(lvi);
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void readAllMantra()
        {
            LogHelper.Debug("readAllMantra");
            try
            {
                foreach (KeyValuePair<string, Mantra> kv in Data.Get<Mantra>())
                {

                    ListViewItem lvi = new ListViewItem();

                    lvi.Text = kv.Key;
                    lvi.SubItems.Add(kv.Value.Name);
                    lvi.SubItems.Add(EnumData.GetDisplayName(kv.Value.RequireAttribute));
                    lvi.SubItems.Add(kv.Value.RequireValue.ToString());

                    String MantraRunEffectDescription = "";
                    for (int i = 0; i < kv.Value.MantraRunEffectDescription.Count; i++)
                    {
                        MantraRunEffectDescription += kv.Value.MantraRunEffectDescription[i].EffectDescription + ";";
                    }
                    lvi.SubItems.Add(MantraRunEffectDescription);

                    MantraListView.Items.Add(lvi);
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void readAllGender()
        {
            LogHelper.Debug("readAllGender");
            try
            {
                GenderComboBox.DisplayMember = "value";
                GenderComboBox.ValueMember = "key";
                foreach (Gender gender in Enum.GetValues(typeof(Gender)))
                {

                    ComboBoxItem cbi = new ComboBoxItem(((int)gender).ToString(), EnumData.GetDisplayName(gender));
                    GenderComboBox.Items.Add(cbi);
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void readAllQuestState()
        {
            LogHelper.Debug("readAllQuestState");
            try
            {
                QuestStateComboBox.DisplayMember = "value";
                QuestStateComboBox.ValueMember = "key";
                foreach (QuestState questState in Enum.GetValues(typeof(QuestState)))
                {

                    ComboBoxItem cbi = new ComboBoxItem(((int)questState).ToString(), questState.ToString());
                    QuestStateComboBox.Items.Add(cbi);
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void readShowAllQuest()
        {
            LogHelper.Debug("readShowAllQuest");
            try
            {
                ShowAllQuestComboBox.DisplayMember = "value";
                ShowAllQuestComboBox.ValueMember = "key";
                foreach (showAllQuest showAllQuest in Enum.GetValues(typeof(showAllQuest)))
                {

                    ComboBoxItem cbi = new ComboBoxItem(((int)showAllQuest).ToString(), showAllQuest.ToString());
                    ShowAllQuestComboBox.Items.Add(cbi);
                }
                ShowAllQuestComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void readAllBook()
        {
            LogHelper.Debug("readAllBook");
            try
            {
                foreach (KeyValuePair<string, Book> kv in Data.Get<Book>())
                {

                    ListViewItem lvi = new ListViewItem();

                    lvi.Text = kv.Key;
                    lvi.SubItems.Add(kv.Value.Name);
                    lvi.SubItems.Add(EnumData.GetDisplayName(kv.Value.BookTab));
                    lvi.SubItems.Add(kv.Value.MaxReadTime.ToString());
                    lvi.SubItems.Add(kv.Value.ReadConditionDescription);
                    lvi.SubItems.Add(getBaseFlowGraphStr(kv.Value.ShowCondition));


                    BookListView.Items.Add(lvi);
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void readDatas()
        {
            LogHelper.Debug("readDatas");
            try
            {
                initDatas();

                readCommonData();
                readInventory();
                readAllSkill();
                readExteriorName();
                readCommunity();
                readParty();
                readFlag();
                readFlagLove();
                readQuest();
                readElective();
                readNurturanceOrder();
                readBook();
                readAlchemy();
                readForge();
                readShop();
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void initDatas()
        {
            LogHelper.Debug("initDatas");
            try
            {
                InventoryListView.SelectedItems.Clear();
                CharacterListView.SelectedItems.Clear();
                ElementComboBox.SelectedIndex = -1;
                SpecialSkillComboBox.SelectedIndex = -1;
                GrowthFactorTextBox.Text = "";
                HpTextBox.Text = "";
                MaxHpTextBox.Text = "";
                MpTextBox.Text = "";
                MaxMpTextBox.Text = "";
                AttackTextBox.Text = "";
                DefenseTextBox.Text = "";
                HitTextBox.Text = "";
                MoveTextBox.Text = "";
                DodgeTextBox.Text = "";
                ParryTextBox.Text = "";
                CriticalTextBox.Text = "";
                CounterTextBox.Text = "";
                AffiliationStrTextBox.Text = "";
                AffiliationTextBox.Text = "";
                WeaponComboBox.SelectedIndex = -1;
                WeaponComboBox2.SelectedIndex = -1;
                ClothComboBox.SelectedIndex = -1;
                JewelryComboBox.SelectedIndex = -1;
                StrTextBox.Text = "";
                StrLevelTextBox.Text = "";
                StrExtraTextBox.Text = "";
                VitTextBox.Text = "";
                VitLevelTextBox.Text = "";
                VitExtraTextBox.Text = "";
                DexTextBox.Text = "";
                DexLevelTextBox.Text = "";
                DexExtraTextBox.Text = "";
                SpiTextBox.Text = "";
                SpiLevelTextBox.Text = "";
                SpiExtraTextBox.Text = "";
                VibrantTextBox.Text = "";
                CultivatedTextBox.Text = "";
                ResoluteTextBox.Text = "";
                BraveTextBox.Text = "";
                ZitherTextBox.Text = "";
                ChessTextBox.Text = "";
                CalligraphyTextBox.Text = "";
                PaintingTextBox.Text = "";

                HavingSkillListView.Items.Clear();
                EquipSkill1Label.Text = "无";
                EquipSkill2Label.Text = "无";
                EquipSkill3Label.Text = "无";
                EquipSkill4Label.Text = "无";

                TraitListView.SelectedItems.Clear();
                HavingTraitListView.Items.Clear();

                MantraListView.SelectedItems.Clear();
                HavingMantraListView.Items.Clear();
                WorkMantraLabel.Text = "无";
                MantraCurrentLevelTextBox.Text = "";
                MantraMaxLevelTextBox.Text = "";

                CharacterExteriorListView.SelectedItems.Clear();
                SurNameTextBox.Text = "";
                NameTextBox.Text = "";
                NicknameTextBox.Text = "";
                ProtraitTextBox.Text = "";
                ModelTextBox.Text = "";
                DescriptionTextBox.Text = "";

                CommunityListView.SelectedItems.Clear();
                CommunityMaxLevelTextBox.Text = "";
                CommunityLevelTextBox.Text = "";
                CommunityExpTextBox.Text = "";
                CommunityIsOpenCheckBox.Checked = false;

                PartyListView.SelectedItems.Clear();

                FlagListView.SelectedItems.Clear();

                ctb_MasterLoveTextBox.Text = "";
                dxl_MasterLoveTextBox.Text = "";
                dh_MasterLoveTextBox.Text = "";
                ht_MasterLoveTextBox.Text = "";
                fxlh_MasterLoveTextBox.Text = "";
                lxp_MasterLoveTextBox.Text = "";
                ncc_MasterLoveTextBox.Text = "";
                tsz_MasterLoveTextBox.Text = "";
                mrx_MasterLoveTextBox.Text = "";
                j_MasterLoveTextBox.Text = "";
                xx_NpcLoveTextBox.Text = "";

                QuestListView.SelectedItems.Clear();
                QuestStateComboBox.SelectedIndex = -1;

                ElectiveListView.SelectedItems.Clear();
                CurrentElectiveLabel.Text = "";

                NurturanceOrderListView.SelectedItems.Clear();

                BookListView.SelectedItems.Clear();
                HavingBookListView.SelectedItems.Clear();

                AlchemyListView.SelectedItems.Clear();

                ForgeFightListView.SelectedItems.Clear();
                ForgeBladeAndSwordListView.SelectedItems.Clear();
                ForgeLongAndShortListView.SelectedItems.Clear();
                ForgeQimenListView.SelectedItems.Clear();
                ForgeArmorListView.SelectedItems.Clear();

                ShopListView.SelectedItems.Clear();

                foreach(KeyValuePair<string,CharacterInfoData> kv in gameData.Character)
                {
                    CharacterInfoData cid = kv.Value;
                    AttachPropsEffect(Data.Get<Props>(cid.Equip[EquipType.Weapon]), cid);
                    AttachPropsEffect(Data.Get<Props>(cid.Equip[EquipType.Cloth]), cid);
                    AttachPropsEffect(Data.Get<Props>(cid.Equip[EquipType.Jewelry]), cid);
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }

        }

        private void readCommonData()
        {
            LogHelper.Debug("readCommonData");
            try
            {
                GameVersionTextBox.Text = gameData.GameVersion;
                string text = pathOfWuxiaSaveHeader.SaveTime.ToString();
                text = text.Replace("/星期一", "").Replace("/星期二", "").Replace("/星期三", "").Replace("/星期四", "").Replace("/星期五", "").Replace("/星期六", "").Replace("/星期日", "").Replace("/周一", "").Replace("/周二", "").Replace("/周三", "").Replace("/周四", "").Replace("/周五", "").Replace("/周六", "").Replace("/周日", "");
                SaveTimeDateTimePicker.Text = text;
                CurrentMapComboBox.SelectedIndex = CurrentMapComboBox.Items.IndexOf(dcbi[gameData.MapId]);
                PlayerPostioionTextBox.Text = gameData.PlayerPostioion.ToString();
                PlayerForwardTextBox.Text = gameData.PlayerForward.ToString();
                CurrentYearComboBox.SelectedIndex = gameData.Round.CurrentYear - 1;
                CurrentMonthComboBox.SelectedIndex = gameData.Round.CurrentMonth - 1;
                CurrentRoundOfMonthComboBox.SelectedIndex = gameData.Round.CurrentRoundOfMonth - 1;
                CurrentTimeComboBox.SelectedIndex = gameData.Round.CurrentTime - 1;
                CurrentRoundTextBox.Text = gameData.Round.CurrentRound.ToString();
                EmotionTextBox.Text = gameData.emotion.ToString();
                MoneyTextBox.Text = gameData.Money.ToString();
                GameLevelComboBox.SelectedIndex = (int)gameData.GameLevel - 1;
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void readInventory()
        {
            LogHelper.Debug("readInventory");
            try
            {
                InventoryListView.Items.Clear();
                InventoryListView.BeginUpdate();

                foreach (KeyValuePair<string, InventoryData> kv in gameData.Inventory)
                {


                    if (!string.IsNullOrEmpty(kv.Key))
                    {
                        ListViewItem lvi = new ListViewItem();
                        lvi.Text = kv.Key;

                        if (Data.Get<Props>(kv.Key) != null)
                        {
                            lvi.SubItems.Add(Data.Get<Props>(kv.Key).Name);
                            lvi.SubItems.Add(kv.Value.Count.ToString());
                            InventoryListView.Items.Add(lvi);
                        }
                    }
                }

                InventoryListView.EndUpdate();  //结束数据处理，UI界面一次性绘制。 
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void readCommunity()
        {
            LogHelper.Debug("readCommunity");
            try
            {
                CommunityListView.Items.Clear();

                foreach (KeyValuePair<string, CommunityData> kv in gameData.Community)
                {
                    if (kv.Key == "Player")
                    {
                        continue;
                    }

                    ListViewItem lvi = new ListViewItem();

                    lvi.Text = kv.Key;

                    lvi.SubItems.Add(gameData.Exterior[kv.Key].FullName());

                    CommunityListView.Items.Add(lvi);
                }

                CommunityListView.EndUpdate();  //结束数据处理，UI界面一次性绘制。 
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void readParty()
        {
            LogHelper.Debug("readParty");
            try
            {
                PartyListView.Items.Clear();

                foreach (string id in gameData.Party)
                {

                    ListViewItem lvi = new ListViewItem();

                    lvi.Text = id;

                    lvi.SubItems.Add(gameData.Exterior[id].FullName());

                    PartyListView.Items.Add(lvi);
                }

                PartyListView.EndUpdate();  //结束数据处理，UI界面一次性绘制。 
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void readFlag()
        {
            LogHelper.Debug("readFlag");
            try
            {
                FlagListView.Items.Clear();

                foreach (KeyValuePair<string, int> kv in gameData.Flag)
                {
                    ListViewItem lvi = new ListViewItem();

                    lvi.Text = kv.Key;

                    lvi.SubItems.Add(kv.Value.ToString());
                    lvi.SubItems.Add("");

                    FlagListView.Items.Add(lvi);
                }

                FlagListView.EndUpdate();  //结束数据处理，UI界面一次性绘制。 

                readFlagRemark();
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void readFlagRemark()
        {
            LogHelper.Debug("readFlagRemark");
            try
            {
                if (File.Exists(FlagRemarkFilePath))
                {
                    StreamReader sr = new StreamReader(FlagRemarkFilePath);
                    string line;

                    // 从文件读取并显示行，直到文件的末尾 
                    while ((line = sr.ReadLine()) != null)
                    {
                        string[] flag = line.Split(':');
                        ListViewItem lvi = FlagListView.FindItemWithText(flag[0]);
                        if (lvi != null)
                        {
                            if (lvi.SubItems.Count < 3)
                            {
                                lvi.SubItems.Add(flag[1]);
                            }
                            else
                            {
                                lvi.SubItems[2].Text = flag[1];
                            }
                        }
                    }

                    sr.Close();
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void readFlagLove()
        {
            LogHelper.Debug("readFlagLove");
            try
            {
                ctb_MasterLoveTextBox.Text = Game.GameData.Flag["fg0201_MasterLove"].ToString();
                dxl_MasterLoveTextBox.Text = Game.GameData.Flag["fg0202_MasterLove"].ToString();
                dh_MasterLoveTextBox.Text = Game.GameData.Flag["fg0203_MasterLove"].ToString();
                lxp_MasterLoveTextBox.Text = Game.GameData.Flag["fg0204_MasterLove"].ToString();
                ht_MasterLoveTextBox.Text = Game.GameData.Flag["fg0205_MasterLove"].ToString();
                tsz_MasterLoveTextBox.Text = Game.GameData.Flag["fg0206_MasterLove"].ToString();
                fxlh_MasterLoveTextBox.Text = Game.GameData.Flag["fg0207_MasterLove"].ToString();
                ncc_MasterLoveTextBox.Text = Game.GameData.Flag["fg0208_MasterLove"].ToString();
                mrx_MasterLoveTextBox.Text = Game.GameData.Flag["fg0209_MasterLove"].ToString();
                j_MasterLoveTextBox.Text = Game.GameData.Flag["fg0210_MasterLove"].ToString();
                xx_NpcLoveTextBox.Text = Game.GameData.Flag["fg0301_NpcLove"].ToString();
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void readQuest()
        {
            LogHelper.Debug("readQuest");
            try
            {
                QuestListView.Items.Clear();

                List<Quest> list = this.Data.Get<Quest>().Values.ToList();
                if (ShowAllQuestComboBox.SelectedIndex == 0)
                {
                    list = list.FindAll((Quest x) => x.Type == QuestType.Teacher || x.Type == QuestType.EveryDay || x.Type == QuestType.Emergency || x.Type == QuestType.Working || x.Type == QuestType.Invitation);
                }


                foreach (Quest quest in list)
                {

                    ListViewItem lvi = new ListViewItem();

                    lvi.Text = quest.Id;
                    lvi.SubItems.Add(quest.Name);
                    lvi.SubItems.Add(quest.Brief);
                    lvi.SubItems.Add(getBaseFlowGraphStr(quest.ShowCondition));
                    lvi.SubItems.Add(getBaseFlowGraphStr(quest.PickUpCondition));
                    lvi.SubItems.Add(quest.DeadLine);
                    lvi.SubItems.Add(((QuestSchedule)quest.Schedule).ToString());

                    String EvaluationReward = "";
                    if (quest.EvaluationReward != null)
                    {
                        foreach (KeyValuePair<EvaluationLevel, EvaluationReward> kv in quest.EvaluationReward)
                        {
                            if (kv.Value != null)
                            {
                                if (kv.Value.Id == "Money")
                                {
                                    EvaluationReward += kv.Value.Count + "钱,";
                                }
                                else if (!string.IsNullOrEmpty(kv.Value.Id))
                                {
                                    EvaluationReward += Data.Get<Props>(kv.Value.Id).Name + "*" + kv.Value.Count + ",";
                                }
                            }
                        }
                        EvaluationReward = EvaluationReward.Substring(0, Math.Max(0, EvaluationReward.Length - 1));
                    }
                    lvi.SubItems.Add(EvaluationReward);

                    QuestListView.Items.Add(lvi);
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void readElective()
        {
            LogHelper.Debug("readElective");
            try
            {
                ElectiveListView.Items.Clear();

                foreach (KeyValuePair<string, Elective> kv in Data.Get<Elective>())
                {

                    ListViewItem lvi = new ListViewItem();

                    lvi.Text = kv.Value.Id;
                    lvi.SubItems.Add(kv.Value.Name);
                    lvi.SubItems.Add(EnumData.GetDisplayName(kv.Value.Grade));
                    lvi.SubItems.Add(kv.Value.ConditionDescription);
                    lvi.SubItems.Add((Game.GameData.Elective.Triggered.Contains(kv.Value.Id) ? ElectiveState.已进修 : ElectiveState.未进修).ToString());


                    ElectiveListView.Items.Add(lvi);
                }

                if (!string.IsNullOrEmpty(Game.GameData.Elective.Id))
                {
                    string[] electives = Game.GameData.Elective.Id.Split('_');
                    string electiveStr = "";
                    for (int i = 0; i < electives.Length; i++)
                    {
                        electiveStr += Data.Get<Elective>(electives[i]).Name + ",";
                    }
                    electiveStr = electiveStr.Substring(0, electiveStr.Length - 1);
                    CurrentElectiveLabel.Text = electiveStr;
                }
                else
                {
                    CurrentElectiveLabel.Text = "无";
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void readNurturanceOrder()
        {
            LogHelper.Debug("readNurturanceOrder");
            try
            {
                NurturanceOrderListView.Items.Clear();

                Game.GameData.NurturanceOrder.CteateDevelopOrderTree();
                foreach (KeyValuePair<string, Nurturance> kv in Data.Get<Nurturance>())
                {

                    ListViewItem lvi = new ListViewItem();

                    lvi.Text = kv.Value.Id;
                    string name = "";

                    for (int i = 0; i < lvi.Text.Split('_').Length - 2; i++)
                    {
                        name += "  ";
                    }
                    name += kv.Value.Name;
                    lvi.SubItems.Add(name);
                    lvi.SubItems.Add(getNurturanceOrderContain(Game.GameData.NurturanceOrder.Root, lvi.Text) ? "开启" : "关闭");
                    lvi.SubItems.Add(EnumData.GetDisplayName(kv.Value.Fuction));
                    lvi.SubItems.Add(EnumData.GetDisplayName(kv.Value.UIType));
                    lvi.SubItems.Add(kv.Value.Emotion.ToString());
                    lvi.SubItems.Add(getBaseFlowGraphStr(kv.Value.ShowCondition));
                    lvi.SubItems.Add(getBaseFlowGraphStr(kv.Value.OpenCondition));
                    lvi.SubItems.Add(getBaseFlowGraphStr(kv.Value.AdditionCondition));
                    lvi.SubItems.Add(kv.Value.AdditionValue.ToString() + "%");


                    NurturanceOrderListView.Items.Add(lvi);
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }

        }
        private void readBook()
        {
            LogHelper.Debug("readBook");
            try
            {
                HavingBookListView.Items.Clear();

                foreach (KeyValuePair<string, BookData> kv in Game.GameData.ReadBookManager)
                {

                    ListViewItem lvi = new ListViewItem();

                    lvi.Text = kv.Value.Id;
                    lvi.SubItems.Add(kv.Value.Item.Name);
                    lvi.SubItems.Add(kv.Value.IsReadFinish ? "是" : "否");
                    lvi.SubItems.Add(Mathf.Max(0, kv.Value.Item.MaxReadTime - kv.Value.CurrentReadTime).ToString());


                    HavingBookListView.Items.Add(lvi);
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void readAlchemy()
        {
            LogHelper.Debug("readAlchemy");
            try
            {
                AlchemyListView.Items.Clear();

                foreach (KeyValuePair<string, Alchemy> kv in Data.Get<Alchemy>())
                {
                    Props prop = Data.Get<Props>(kv.Value.PropsId);

                    ListViewItem lvi = new ListViewItem();

                    lvi.Text = kv.Key;
                    lvi.SubItems.Add(prop.Name);
                    lvi.SubItems.Add(prop.PropsEffectDescription);
                    lvi.SubItems.Add(Game.GameData.Alchemy.Learned.Contains(kv.Key) ? "是" : "否");


                    AlchemyListView.Items.Add(lvi);
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void readForge()
        {
            LogHelper.Debug("readForge");
            try
            {
                ForgeFightListView.Items.Clear();
                ForgeBladeAndSwordListView.Items.Clear();
                ForgeLongAndShortListView.Items.Clear();
                ForgeQimenListView.Items.Clear();
                ForgeArmorListView.Items.Clear();

                foreach (KeyValuePair<string, Forge> kv in Data.Get<Forge>())
                {
                    Props prop = Data.Get<Props>(kv.Value.PropsId);

                    ListViewItem lvi = new ListViewItem();

                    lvi.Text = kv.Key;
                    lvi.SubItems.Add(prop.Name);
                    lvi.SubItems.Add(EnumData.GetDisplayName(prop.PropsCategory));
                    lvi.SubItems.Add(prop.PropsEffectDescription);
                    lvi.SubItems.Add(Game.GameData.Forge.Opened.Contains(kv.Key) ? "是" : "否");
                    lvi.SubItems.Add(kv.Value.OpenRound.ToString());


                    switch (prop.PropsCategory)
                    {
                        case Heluo.Data.PropsCategory.Fist:
                        case Heluo.Data.PropsCategory.Leg:
                            ForgeFightListView.Items.Add(lvi);
                            break;
                        case Heluo.Data.PropsCategory.Sword:
                        case Heluo.Data.PropsCategory.Blade:
                            ForgeBladeAndSwordListView.Items.Add(lvi);
                            break;
                        case Heluo.Data.PropsCategory.Long:
                        case Heluo.Data.PropsCategory.Short:
                            ForgeLongAndShortListView.Items.Add(lvi);
                            break;
                        case Heluo.Data.PropsCategory.DualWielding:
                        case Heluo.Data.PropsCategory.Special:
                            ForgeQimenListView.Items.Add(lvi);
                            break;
                        default:
                            ForgeArmorListView.Items.Add(lvi);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void readShop()
        {
            LogHelper.Debug("readShop");
            try
            {
                ShopListView.Items.Clear();

                foreach (KeyValuePair<string, Shop> kv in Data.Get<Shop>())
                {
                    Props prop = Data.Get<Props>(kv.Value.PropsId);

                    ListViewItem lvi = new ListViewItem();

                    lvi.Text = kv.Key;
                    lvi.SubItems.Add(prop.Name);
                    lvi.SubItems.Add(prop.PropsEffectDescription);
                    lvi.SubItems.Add(getBaseFlowGraphStr(kv.Value.Condition));
                    lvi.SubItems.Add(kv.Value.IsRepeat ? "是" : "否");

                    string ShopPeriods = "";
                    for (int i = 0; i < kv.Value.ShopPeriods.Count; i++)
                    {
                        ShopPeriods += kv.Value.ShopPeriods[i].OpenRound + "-" + (kv.Value.ShopPeriods[i].CloseRound - 1) + ";";
                    }
                    ShopPeriods = ShopPeriods.Substring(0, ShopPeriods.Length - 1);
                    lvi.SubItems.Add(ShopPeriods);

                    lvi.SubItems.Add(shopIsSoldOut(kv.Value));

                    ShopListView.Items.Add(lvi);
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        public string shopIsSoldOut(Shop shop)
        {
            LogHelper.Debug("shopIsSoldOut");
            try
            {

                if (shop.ShopPeriods != null && shop.ShopPeriods.Count != 0)
                {
                    for (int j = 0; j < shop.ShopPeriods.Count; j++)
                    {
                        ShopPeriod shopPeriod = shop.ShopPeriods[j];
                        if (shopPeriod.CheckInPeriod(Game.GameData.Round.CurrentRound))
                        {
                            return Game.GameData.Shop.CheckIsSoldOut(shop.Id, shopPeriod) && !shop.IsRepeat ? "是" : "否";
                        }
                    }
                }
                return "";
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
                return "";
            }
        }

        private void listView1_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("listView1_GotFocus");
            try
            {
                if (saveFileIsSelected)
                {
                    InventoryAdd1button.Enabled = true;
                    InventoryAdd10button.Enabled = true;
                    InventorySub1button.Enabled = false;
                    InventorySub10button.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void listView2_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("listView2_GotFocus");
            try
            {
                if (saveFileIsSelected)
                {
                    InventoryAdd1button.Enabled = false;
                    InventoryAdd10button.Enabled = false;
                    InventorySub1button.Enabled = true;
                    InventorySub10button.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void InventoryAdd1button_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("InventoryAdd1button_Click");
            try
            {
                messageLabel.Text = "";
                foreach (ListViewItem lvi in PropsListView.SelectedItems)  //选中项遍历  
                {
                    if (!saveFileIsSelected)
                    {
                        messageLabel.Text = "请先选择一个存档";
                        LogHelper.Debug("请先选择一个存档");
                        PropsListView.SelectedItems.Clear();
                        return;
                    }
                    ListViewItem havinglvi = InventoryListView.FindItemWithText(lvi.Text);

                    if (havinglvi == null)
                    {
                        havinglvi = new ListViewItem();

                        havinglvi.Text = lvi.Text;

                        havinglvi.SubItems.Add(Data.Get<Props>(lvi.Text).Name);
                        havinglvi.SubItems.Add(1.ToString());

                        this.InventoryListView.Items.Add(havinglvi);
                    }
                    else
                    {
                        ListViewSubItem si = havinglvi.SubItems[2];
                        int num = int.Parse(si.Text) + 1;
                        if (num > 99)
                        {
                            num = 99;
                        }
                        si.Text = num.ToString();
                    }

                    gameData.Inventory.Add(lvi.Text);
                    InventoryListView.EndUpdate();  //结束数据处理，UI界面一次性绘制。 
                    InventoryListView.Items[havinglvi.Index].Selected = true;
                    InventoryListView.EnsureVisible(havinglvi.Index);
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void InventoryAdd10button_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("InventoryAdd10button_Click");
            try
            {
                messageLabel.Text = "";
                foreach (ListViewItem lvi in PropsListView.SelectedItems)  //选中项遍历  
                {
                    if (!saveFileIsSelected)
                    {
                        messageLabel.Text = "请先选择一个存档";
                        LogHelper.Debug("请先选择一个存档");
                        PropsListView.SelectedItems.Clear();
                        return;
                    }
                    ListViewItem havinglvi = InventoryListView.FindItemWithText(lvi.Text);

                    if (havinglvi == null)
                    {
                        havinglvi = new ListViewItem();

                        havinglvi.Text = lvi.Text;

                        havinglvi.SubItems.Add(Data.Get<Props>(lvi.Text).Name);
                        havinglvi.SubItems.Add(10.ToString());

                        InventoryListView.Items.Add(havinglvi);
                    }
                    else
                    {
                        ListViewSubItem si = havinglvi.SubItems[2];
                        int num = int.Parse(si.Text) + 10;
                        if (num > 99)
                        {
                            num = 99;
                        }
                        si.Text = num.ToString();
                    }

                    gameData.Inventory.Add(lvi.Text, 10);
                    InventoryListView.EndUpdate();  //结束数据处理，UI界面一次性绘制。 
                    InventoryListView.Items[havinglvi.Index].Selected = true;
                    InventoryListView.EnsureVisible(havinglvi.Index);
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void InventorySub1button_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("InventorySub1button_Click");
            try
            {
                messageLabel.Text = "";
                foreach (ListViewItem lvi in InventoryListView.SelectedItems)  //选中项遍历  
                {
                    if (!saveFileIsSelected)
                    {
                        messageLabel.Text = "请先选择一个存档";
                        LogHelper.Debug("请先选择一个存档");
                        PropsListView.SelectedItems.Clear();
                        return;
                    }
                    ListViewSubItem si = lvi.SubItems[2];
                    int num = int.Parse(si.Text) - 1;
                    if (num < 0)
                    {
                        num = 0;
                    }

                    si.Text = num.ToString();
                    gameData.Inventory.Remove(lvi.Text);
                    InventoryListView.EndUpdate();  //结束数据处理，UI界面一次性绘制。
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void InventorySub10button_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("InventorySub10button_Click");
            try
            {
                messageLabel.Text = "";
                foreach (ListViewItem lvi in InventoryListView.SelectedItems)  //选中项遍历  
                {
                    if (!saveFileIsSelected)
                    {
                        messageLabel.Text = "请先选择一个存档";
                        LogHelper.Debug("请先选择一个存档");
                        PropsListView.SelectedItems.Clear();
                        return;
                    }
                    ListViewSubItem si = lvi.SubItems[2];
                    int num = int.Parse(si.Text) - 10;
                    if (num < 0)
                    {
                        num = 0;
                    }

                    si.Text = num.ToString();
                    gameData.Inventory.Remove(lvi.Text, 10);
                    InventoryListView.EndUpdate();  //结束数据处理，UI界面一次性绘制。 
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void createFormula(CharacterInfoData cid)
        {
            LogHelper.Debug("createFormula");
            try
            {
                cid.CreateFormula();
                if (gameData.Community != null && gameData.Community.ContainsKey(cid.Id))
                {
                    if (cid.status_coefficient_of_community == null)
                    {
                        cid.status_coefficient_of_community = Game.Data.Get<GameFormula>("status_coefficient_of_community_" + cid.Id);

                        if (cid.status_coefficient_of_community != null)
                        {
                            if (cid.status_coefficient_of_community.Formula._script == null)
                            {
                                cid.status_coefficient_of_community.Formula._script = new Script(CoreModules.Math);
                            }
                            cid.status_coefficient_of_community.Formula._script.DoString(string.Concat(new string[]
                                       {
                                            "function ",
                                            cid.status_coefficient_of_community.Id,
                                            "() return ",
                                            cid.status_coefficient_of_community.Formula.Expression,
                                            " end"
                                       }), null, null);
                        }
                    }
                    int level = Game.GameData.Community[cid.Id].Favorability.Level;
                    if (cid.CommunityFormulaProperty == null)
                    {
                        cid.CommunityFormulaProperty = new Dictionary<string, int>
                    {
                        {
                            "community_lv",
                            level
                        }
                    };
                    }
                    else if (cid.CommunityFormulaProperty.ContainsKey("community_lv"))
                    {
                        cid.CommunityFormulaProperty["community_lv"] = level;
                    }
                    else
                    {
                        cid.CommunityFormulaProperty.Add("community_lv", level);
                    }
                }
                Dictionary<string, int> baseFormulaProperty = cid.GetBaseFormulaProperty();

                foreach (object obj in Enum.GetValues(typeof(CharacterProperty)))
                {
                    CharacterProperty index = (CharacterProperty)obj;
                    string key = string.Format("basic_{0}", index.ToString().ToLower());
                    if (CharacterInfoData.PropertyFormula.ContainsKey(key))
                    {
                        GameFormula gameFormula = CharacterInfoData.PropertyFormula[key];


                        if (gameFormula.Formula._script == null)
                        {
                            gameFormula.Formula._script = new Script(CoreModules.Math);
                        }
                        foreach (KeyValuePair<string, int> keyValuePair in baseFormulaProperty)
                        {
                            gameFormula.Formula._script.Globals.Set(keyValuePair.Key, DynValue.NewNumber(keyValuePair.Value));
                        }
                        gameFormula.Formula._script.DoString(string.Concat(new string[]
                           {
                                            "function ",
                                            gameFormula.Id,
                                            "() return ",
                                            gameFormula.Formula.Expression,
                                            " end"
                           }), null, null);
                    }
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }
        private void characterListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            LogHelper.Debug("characterListView_SelectedIndexChanged");
            messageLabel.Text = "";
            try
            {
                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {  //选中项遍历 
                    if (!saveFileIsSelected)
                    {
                        messageLabel.Text = "请先选择一个存档";
                        LogHelper.Debug("请先选择一个存档");
                        CharacterListView.SelectedItems.Clear();
                        return;
                    }
                    string id = lvi.Text;
                    if (id == "in0101")
                    {
                        id = "Player";
                    }
                    CharacterInfoData cid = new CharacterInfoData();
                    if (!gameData.Character.ContainsKey(id))
                    {
                        CharacterInfo characterInfo = Data.Get<CharacterInfo>(id);
                        if (characterInfo != null)
                        {
                            cid = new CharacterInfoData(characterInfo);

                            createFormula(cid);

                            cid.OnRoundChange(gameData.Round.CurrentRound, false);
                            gameData.Character.Add(id, cid);
                        }
                    }
                    else
                    {
                        cid = gameData.Character[id];
                    }
                    createFormula(cid);
                    readSelectCharacterData(cid);
                    readAllSkill();

                    if (gameData.Community.ContainsKey(lvi.Text))
                    {
                        GrowthFactorTextBox.Enabled = false;
                    }
                    else
                    {
                        GrowthFactorTextBox.Enabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        public void readSelectCharacterData(CharacterInfoData cid)
        {
            LogHelper.Debug("readSelectCharacterData");
            try
            {
                readCharacterInfoData(cid);
                readCharacterSkillData(cid);
                updateSkillPredictionDamage(cid);
                readCharacterEquipSkillData(cid);
                readCharacterTraitData(cid);
                readCharacterMantraData(cid);
                readCharacterWorkMantraData(cid);

                SkillCurrentLevelTextBox.Text = "";
                SkillMaxLevelTextBox.Text = "";
                MantraCurrentLevelTextBox.Text = "";
                MantraMaxLevelTextBox.Text = "";
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        public void readCharacterInfoData(CharacterInfoData cid)
        {
            LogHelper.Debug("readCharacterInfoData");
            try
            {
                readCharacterProperty(cid);

                ElementComboBox.SelectedIndex = (int)cid.Element;
                if (!string.IsNullOrEmpty(cid.SpecialSkill))
                {
                    SpecialSkillComboBox.SelectedIndex = SpecialSkillComboBox.Items.IndexOf(dcbi[cid.SpecialSkill]);
                }
                else
                {
                    SpecialSkillComboBox.SelectedIndex = -1;
                }

                if(cid.Id != "Player" && gameData.Community.ContainsKey(cid.Id))
                {
                    int level = gameData.Community[cid.Id].Favorability.Level;
                    if (cid.CommunityFormulaProperty == null)
                    {
                        cid.CommunityFormulaProperty = new Dictionary<string, int>
                    {
                        {
                            "community_lv",
                            level
                        }
                    };
                    }
                    else if (cid.CommunityFormulaProperty.ContainsKey("community_lv"))
                    {
                        cid.CommunityFormulaProperty["community_lv"] = level;
                    }
                    else
                    {
                        cid.CommunityFormulaProperty.Add("community_lv", level);
                    }
                        if (cid.status_coefficient_of_community == null)
                        {
                            cid.status_coefficient_of_community = Game.Data.Get<GameFormula>("status_coefficient_of_community_" + cid.Id);
                        }
                        GrowthFactorTextBox.Text = (cid.status_coefficient_of_community.Evaluate(cid.CommunityFormulaProperty) + 1).ToString();
                }
                else
                {
                    GrowthFactorTextBox.Text = cid.GrowthFactor.ToString();
                }

                StrTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Str].Value.ToString();
                StrLevelTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Str].Level.ToString();
                StrExtraTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Str].Extra.ToString();

                VitTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Vit].Value.ToString();
                VitLevelTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Vit].Level.ToString();
                VitExtraTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Vit].Extra.ToString();

                DexTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Dex].Value.ToString();
                DexLevelTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Dex].Level.ToString();
                DexExtraTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Dex].Extra.ToString();

                SpiTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Spi].Value.ToString();
                SpiLevelTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Spi].Level.ToString();
                SpiExtraTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Spi].Extra.ToString();

                VibrantTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Vibrant].Value.ToString();
                CultivatedTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Cultivated].Value.ToString();
                ResoluteTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Resolute].Value.ToString();
                BraveTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Brave].Value.ToString();
                ZitherTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Zither].Value.ToString();
                ChessTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Chess].Value.ToString();
                CalligraphyTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Calligraphy].Value.ToString();
                PaintingTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Painting].Value.ToString();

                //WeaponComboBox.SelectedIndex = -1;
                if (!string.IsNullOrEmpty(cid.Equip[EquipType.Weapon]) && dcbi.ContainsKey(cid.Equip[EquipType.Weapon]))
                {
                    WeaponComboBox.SelectedIndex = WeaponComboBox.Items.IndexOf(dcbi[cid.Equip[EquipType.Weapon]]);
                }
                else
                {
                    WeaponComboBox.SelectedIndex = -1;
                }
                //weaponComboBox2.SelectedIndex = -1;
                /*if (!string.IsNullOrEmpty(cid.Equip[EquipType.Weapon]))
                {
                    weaponComboBox2.SelectedIndex = weaponComboBox2.Items.IndexOf(dcbi[cid.Equip[EquipType.Weapon]]);
                }*/

                //ClothComboBox.SelectedIndex = -1;
                if (!string.IsNullOrEmpty(cid.Equip[EquipType.Cloth]) && dcbi.ContainsKey(cid.Equip[EquipType.Cloth]))
                {
                    ClothComboBox.SelectedIndex = ClothComboBox.Items.IndexOf(dcbi[cid.Equip[EquipType.Cloth]]);
                }
                else
                {
                    ClothComboBox.SelectedIndex = -1;
                }

                //JewelryComboBox.SelectedIndex = -1;
                if (!string.IsNullOrEmpty(cid.Equip[EquipType.Jewelry]) && dcbi.ContainsKey(cid.Equip[EquipType.Jewelry]))
                {
                    JewelryComboBox.SelectedIndex = JewelryComboBox.Items.IndexOf(dcbi[cid.Equip[EquipType.Jewelry]]);
                }
                else
                {
                    JewelryComboBox.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void readCharacterProperty(CharacterInfoData cid)
        {
            LogHelper.Debug("readCharacterProperty");
            try
            {
                HpTextBox.Text = cid.HP.ToString();
                MaxHpTextBox.Text = cid.Property[CharacterProperty.Max_HP].Value.ToString();
                MpTextBox.Text = cid.MP.ToString();
                MaxMpTextBox.Text = cid.Property[CharacterProperty.Max_MP].Value.ToString();


                AttackTextBox.Text = cid.Property[CharacterProperty.Attack].Value.ToString();
                DefenseTextBox.Text = cid.Property[CharacterProperty.Defense].Value.ToString();
                HitTextBox.Text = cid.Property[CharacterProperty.Hit].Value.ToString();
                MoveTextBox.Text = cid.Property[CharacterProperty.Move].Value.ToString();
                DodgeTextBox.Text = cid.Property[CharacterProperty.Dodge].Value.ToString();
                ParryTextBox.Text = cid.Property[CharacterProperty.Parry].Value.ToString();
                CriticalTextBox.Text = cid.Property[CharacterProperty.Critical].Value.ToString();
                CounterTextBox.Text = cid.Property[CharacterProperty.Counter].Value.ToString();
                AffiliationTextBox.Text = cid.Property[CharacterProperty.Affiliation].Value.ToString();
                if (cid.Property[CharacterProperty.Affiliation].Value > 0)
                {
                    AffiliationStrTextBox.Text = "楚天碧";
                }
                else if (cid.Property[CharacterProperty.Affiliation].Value < 0)
                {
                    AffiliationStrTextBox.Text = "段霄烈";
                }
                else
                {
                    AffiliationStrTextBox.Text = "无";
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        public void readCharacterSkillData(CharacterInfoData cid)
        {
            LogHelper.Debug("readCharacterSkillData");
            try
            {
                HavingSkillListView.Items.Clear();
                foreach (KeyValuePair<string, SkillData> kv in cid.Skill)
                {
                    if (!string.IsNullOrEmpty(kv.Key))
                    {
                        if (string.IsNullOrEmpty(cid.Equip[EquipType.Weapon]) || Data.Get<Props>(cid.Equip[EquipType.Weapon]) == null || Data.Get<Props>(cid.Equip[EquipType.Weapon]).PropsCategory == kv.Value.Item.Type || kv.Value.Item.DamageType == DamageType.Throwing || kv.Value.Item.DamageType == DamageType.Heal)
                        {
                            ListViewItem lvi = new ListViewItem();

                            lvi.Text = kv.Key;

                            lvi.SubItems.Add(kv.Value.Item.Name);

                            HavingSkillListView.Items.Add(lvi);
                        }
                    }
                }

                HavingSkillListView.EndUpdate();  //结束数据处理，UI界面一次性绘制。 
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        public void updateSkillPredictionDamage(CharacterInfoData cid)
        {
            LogHelper.Debug("updateSkillPredictionDamage");
            try
            {
                foreach (ListViewItem lvi in SkillListView.Items)
                {
                    Skill skill = Data.Get<Skill>(lvi.Text);

                    ListViewSubItem ivsi = lvi.SubItems[2];
                    //createFormula(cid);
                    Dictionary<string, int> formulaProperty = cid.GetFormulaProperty();

                    float coefficient = GetCoefficient(formulaProperty, skill, 10);

                    int result = 0;
                    switch (skill.DamageType)
                    {
                        case DamageType.Damage:
                            result = Calculate(skill.Algorithm, (float)cid.Property[CharacterProperty.Attack].Value, coefficient);
                            break;
                        case DamageType.Heal:
                        case DamageType.MpRecover:
                            result = Calculate(skill.Algorithm, 0f, coefficient);
                            break;
                        case DamageType.Summon:
                            result = 0;
                            break;
                        case DamageType.Buff:
                            result = 0;
                            break;
                        case DamageType.Debuff:
                            result = 0;
                            break;
                        case DamageType.Throwing:
                            result = Calculate(skill.Algorithm, (float)cid.Property[CharacterProperty.Attack].Value, coefficient);
                            break;
                    }

                    ivsi.Text = result.ToString();
                }

                SkillListView.EndUpdate();  //结束数据处理，UI界面一次性绘制。 
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        public float GetCoefficient(Dictionary<string, int> dict, Skill skill, int skill_level = 0)
        {
            LogHelper.Debug("GetCoefficient");
            float result;
            try
            {
                GameFormula gameFormula = null;
                gameFormula = Game.Data.Get<GameFormula>(skill.Damage);
                if (gameFormula == null)
                {
                    return 0f;
                }
                int value;
                value = skill_level;
                if (dict.ContainsKey("slv"))
                {
                    dict["slv"] = value;
                }
                else
                {
                    dict.Add("slv", value);
                }
                if (gameFormula.Formula._script == null)
                {
                    gameFormula.Formula._script = new Script(CoreModules.Math);
                }
                foreach (KeyValuePair<string, int> keyValuePair in dict)
                {
                    gameFormula.Formula._script.Globals.Set(keyValuePair.Key, DynValue.NewNumber(keyValuePair.Value));
                }
                gameFormula.Formula._script.DoString(string.Concat(new string[]
                   {
                                            "function ",
                                            gameFormula.Id,
                                            "() return ",
                                            gameFormula.Formula.Expression,
                                            " end"
                   }), null, null);
                result = gameFormula.Evaluate(dict);
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
                return 0f;
            }
            return result;
        }

        public int Calculate(Algorithm algorithm, float value, float coefficient)
        {
            LogHelper.Debug("Calculate");
            try
            {
                switch (algorithm)
                {
                    case Algorithm.Addition:
                        return (int)(value + coefficient);
                    case Algorithm.Subtraction:
                        return (int)(value - coefficient);
                    case Algorithm.Multiplication:
                        return (int)(value * coefficient);
                    case Algorithm.Division:
                        return (int)(value / coefficient);
                    default:
                        return 0;
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
                return 0;
            }
        }

        public void readCharacterEquipSkillData(CharacterInfoData cid)
        {
            LogHelper.Debug("readCharacterEquipSkillData");
            try
            {
                string[] equipSkills = cid.GetEquipSkill();
                for (int i = 0; i < equipSkills.Length; i++)
                {
                    switch (i)
                    {
                        case 0: EquipSkill1Label.Text = string.IsNullOrEmpty(equipSkills[i]) ? "无" : Data.Get<Skill>(equipSkills[i]).Name; break;
                        case 1: EquipSkill2Label.Text = string.IsNullOrEmpty(equipSkills[i]) ? "无" : Data.Get<Skill>(equipSkills[i]).Name; break;
                        case 2: EquipSkill3Label.Text = string.IsNullOrEmpty(equipSkills[i]) ? "无" : Data.Get<Skill>(equipSkills[i]).Name; break;
                        case 3: EquipSkill4Label.Text = string.IsNullOrEmpty(equipSkills[i]) ? "无" : Data.Get<Skill>(equipSkills[i]).Name; break;
                    }
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("saveButton_Click");
            messageLabel.Text = "";
            if (!saveFileIsSelected)
            {
                messageLabel.Text = "请先选择一个存档";
                LogHelper.Debug("请先选择一个存档");
                return;
            }
            string saveFilePath = SaveFilesPathTextBox.Text + "\\" + SaveFileListBox.SelectedItem.ToString();

            fixBug();

            if (File.Exists(saveFilePath))
            {
                File.Delete(saveFilePath);
            }

            FileStream writestream = new FileStream(saveFilePath, FileMode.OpenOrCreate);
            StreamWriter sw = new StreamWriter(writestream);
            try
            {

                if (GameConfig.GameDataHeader == "WUXIASCHOOL_B_1_0")
                {
                    byte[] bytes = Encoding.ASCII.GetBytes("WUXIASCHOOL_B_1_0");
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        memoryStream.Write(bytes, 0, bytes.Length);
                        LZ4MessagePackSerializer.Serialize(memoryStream, pathOfWuxiaSaveHeader, HeluoResolver.Instance);
                        LZ4MessagePackSerializer.Serialize(memoryStream, gameData, HeluoResolver.Instance);

                        sw.BaseStream.Write(memoryStream.ToArray(), 0, memoryStream.ToArray().Length);
                    }
                }
                messageLabel.Text = "保存成功";
                LogHelper.Debug("保存成功");
                sw.Close();
                writestream.Close();
            }
            catch (Exception ex)
            {
                messageLabel.Text = "保存失败。" + ex.Message;
                LogHelper.Debug("保存失败。" + ex.Message);
            }
            finally
            {
                sw.Close();
                writestream.Close();
            }

        }

        private void saveTimeDateTimePicker_ValueChanged(object sender, EventArgs e)
        {
            LogHelper.Debug("saveTimeDateTimePicker_ValueChanged");
            try
            {
                messageLabel.Text = "";
                pathOfWuxiaSaveHeader.SaveTime = DateTime.Parse(SaveTimeDateTimePicker.Text);
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void currentMapComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            LogHelper.Debug("currentMapComboBox_SelectedIndexChanged");
            try
            {
                messageLabel.Text = "";
                if (!isSaveFileSelecting)
                {
                    Map map = Data.Get<Map>(((ComboBoxItem)CurrentMapComboBox.SelectedItem).key);

                    gameData.SetMap(map.Id);
                    gameData.PlayerPostioion = map.DefaultPosition;
                    PlayerPostioionTextBox.Text = map.DefaultPosition.ToString();
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void playerPostioionTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("playerPostioionTextBox_GotFocus");
            try
            {

                PlayerPostioionTextBox.Tag = PlayerPostioionTextBox.Text;
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void playerPostioionTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("playerPostioionTextBox_LostFocus");
            messageLabel.Text = "";
            try
            {
                gameData.PlayerPostioion = stringToVector3(PlayerPostioionTextBox.Text);
                PlayerPostioionTextBox.Text = gameData.PlayerPostioion.ToString();
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
                PlayerPostioionTextBox.Text = PlayerPostioionTextBox.Tag.ToString();
            }
        }

        private void playerForwardTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("playerForwardTextBox_GotFocus");
            try
            {
                PlayerForwardTextBox.Tag = PlayerForwardTextBox.Text;
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void playerForwardTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("playerForwardTextBox_LostFocus");
            try
            {
                messageLabel.Text = "";
                gameData.PlayerForward = stringToVector3(PlayerForwardTextBox.Text);
                PlayerForwardTextBox.Text = gameData.PlayerForward.ToString();
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
                PlayerForwardTextBox.Text = PlayerForwardTextBox.Tag.ToString();
            }
        }

        private Vector3 stringToVector3(string str)
        {
            LogHelper.Debug("stringToVector3");
            str = str.Replace("(", "").Replace(")", "");
            string[] s = str.Split(',');
            Vector3 v = new Vector3(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]));
            return v;
        }

        private void emotionTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("emotionTextBox_GotFocus");
            try
            {
                EmotionTextBox.Tag = EmotionTextBox.Text;
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void emotionTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("emotionTextBox_LostFocus");
            messageLabel.Text = "";
            try
            {
                int emotion = Mathf.Clamp(int.Parse(EmotionTextBox.Text), 0, 100);
                gameData.Emotion = emotion;
                EmotionTextBox.Text = gameData.Emotion.ToString();
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                EmotionTextBox.Text = EmotionTextBox.Tag.ToString();
            }
        }

        private void moneyTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("moneyTextBox_GotFocus");
            try
            {
                MoneyTextBox.Tag = MoneyTextBox.Text;
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void moneyTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("moneyTextBox_LostFocus");
            messageLabel.Text = "";
            try
            {
                int money = Mathf.Clamp(int.Parse(MoneyTextBox.Text), 0, int.MaxValue);
                gameData.Money = money;
                MoneyTextBox.Text = gameData.Money.ToString();
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                MoneyTextBox.Text = MoneyTextBox.Tag.ToString();
            }
        }

        private void gameLevelComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            LogHelper.Debug("gameLevelComboBox_SelectedIndexChanged");
            messageLabel.Text = "";
            try
            {
                gameData.GameLevel = (Heluo.Data.GameLevel)GameLevelComboBox.SelectedIndex + 1;
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void refreshSaveListButton_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("refreshSaveListButton_Click");
            try
            {
                messageLabel.Text = "";

                StreamWriter sw = new StreamWriter(saveFilesPath);
                sw.WriteLine(SaveFilesPathTextBox.Text);
                sw.Close();

                getSaveFiles();
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void elementComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            LogHelper.Debug("elementComboBox_SelectedIndexChanged");
            try
            {
                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];
                    cid.Element = (Element)ElementComboBox.SelectedIndex;
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void specialSkillComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            LogHelper.Debug("specialSkillComboBox_SelectedIndexChanged");
            try
            {
                if (SpecialSkillComboBox.SelectedIndex != -1)
                {
                    foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                    {
                        CharacterInfoData cid = gameData.Character[lvi.Text];

                        cid.SpecialSkill = ((ComboBoxItem)SpecialSkillComboBox.SelectedItem).key;
                    }
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        //private string oldWeaponComboBoxKey = "";
        private void weaponComboBox_TextChanged(object sender, EventArgs e)
        {
            LogHelper.Debug("weaponComboBox_TextChanged");
            try
            {
                WeaponComboBox2.SelectedIndex = WeaponComboBox.SelectedIndex;

                Props oldWeapon = new Props();
                Props newWeapon = new Props();

                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    string oldWeaponComboBoxKey = cid.Equip[EquipType.Weapon];

                    oldWeapon = Data.Get<Props>(cid.Equip[EquipType.Weapon]);

                    if (!string.IsNullOrEmpty(oldWeaponComboBoxKey))
                    {
                        DettachPropsEffect(Data.Get<Props>(oldWeaponComboBoxKey), cid);
                    }

                    if (WeaponComboBox.SelectedIndex != -1)
                    {
                        string newWeaponComboBoxKey = ((ComboBoxItem)WeaponComboBox.SelectedItem).key;
                        cid.Equip[EquipType.Weapon] = newWeaponComboBoxKey;
                        AttachPropsEffect(Data.Get<Props>(newWeaponComboBoxKey), cid);

                        newWeapon = Data.Get<Props>(newWeaponComboBoxKey);
                    }
                    else
                    {
                        //oldWeaponComboBoxKey = "";
                        cid.Equip[EquipType.Weapon] = "";
                    }

                    createFormula(cid);
                    cid.UpgradeProperty(false);
                    readCharacterProperty(cid);
                    if (oldWeapon == null || string.IsNullOrEmpty(oldWeapon.Id) || newWeapon == null || string.IsNullOrEmpty(newWeapon.Id) || (!string.IsNullOrEmpty(oldWeapon.Id) && !string.IsNullOrEmpty(newWeapon.Id) && oldWeapon.PropsCategory != newWeapon.PropsCategory))
                    {
                        readAllSkill();
                        updateSkillPredictionDamage(cid);
                        readCharacterSkillData(cid);
                    }
                    readCharacterEquipSkillData(cid);
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }
        private void weaponComboBox2_TextChanged(object sender, EventArgs e)
        {
            LogHelper.Debug("weaponComboBox2_TextChanged");
            try
            {
                WeaponComboBox.SelectedIndex = WeaponComboBox2.SelectedIndex;
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void AttachPropsEffect(Props prop, CharacterInfoData user)
        {
            LogHelper.Debug("AttachPropsEffect");
            try
            {
                if(prop != null)
                {
                    if (prop.PropsEffect == null)
                    {
                        return;
                    }
                    for (int i = 0; i < prop.PropsEffect.Count; i++)
                    {
                        prop.PropsEffect[i].AttachPropsEffect(user);
                    }
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void DettachPropsEffect(Props porp, CharacterInfoData user)
        {
            LogHelper.Debug("DettachPropsEffect");
            try
            {
                if (porp.PropsEffect == null)
                {
                    return;
                }
                for (int i = 0; i < porp.PropsEffect.Count; i++)
                {
                    porp.PropsEffect[i].DettachPropsEffect(user);
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        //private string oldClothComboBoxKey = "";
        private void clothComboBox_TextChanged(object sender, EventArgs e)
        {
            LogHelper.Debug("clothComboBox_TextChanged");
            try
            {
                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    string oldClothComboBoxKey = cid.Equip[EquipType.Cloth];
                    if (!string.IsNullOrEmpty(oldClothComboBoxKey))
                    {
                        DettachPropsEffect(Data.Get<Props>(oldClothComboBoxKey), cid);
                    }
                    if (ClothComboBox.SelectedIndex != -1)
                    {
                        string newClothComboBoxKey = ((ComboBoxItem)ClothComboBox.SelectedItem).key;
                        cid.Equip[EquipType.Cloth] = newClothComboBoxKey;

                        AttachPropsEffect(Data.Get<Props>(newClothComboBoxKey), cid);

                    }
                    else
                    {
                        //oldClothComboBoxKey = "";
                        cid.Equip[EquipType.Cloth] = "";
                    }

                    //createFormula(cid);
                    cid.UpgradeProperty(false);
                    readCharacterProperty(cid);
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        //private string oldJewelryComboBoxKy = "";
        private void jewelryComboBox_TextChanged(object sender, EventArgs e)
        {
            LogHelper.Debug("jewelryComboBox_TextChanged");
            try
            {
                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    string oldJewelryComboBoxKey = cid.Equip[EquipType.Jewelry];
                    if (!string.IsNullOrEmpty(oldJewelryComboBoxKey))
                    {
                        DettachPropsEffect(Data.Get<Props>(oldJewelryComboBoxKey), cid);
                    }
                    if (JewelryComboBox.SelectedIndex != -1)
                    {
                        string newJewelryComboBoxKey = ((ComboBoxItem)JewelryComboBox.SelectedItem).key;
                        cid.Equip[EquipType.Jewelry] = newJewelryComboBoxKey;
                        AttachPropsEffect(Data.Get<Props>(newJewelryComboBoxKey), cid);

                    }
                    else
                    {
                        //oldJewelryComboBoxKey = "";
                        cid.Equip[EquipType.Jewelry] = "";
                    }

                    //createFormula(cid);
                    cid.UpgradeProperty(false);
                    readCharacterProperty(cid);
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void GrowthFactorTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("GrowthFactorTextBox_GotFocus");
            try
            {
                GrowthFactorTextBox.Tag = GrowthFactorTextBox.Text;
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void GrowthFactorTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("GrowthFactorTextBox_LostFocus");
            try
            {
                if (string.IsNullOrEmpty(GrowthFactorTextBox.Text))
                {
                    GrowthFactorTextBox.Text = "0";
                }

                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    cid.GrowthFactor = float.Parse(GrowthFactorTextBox.Text);

                    GrowthFactorTextBox.Text = cid.GrowthFactor.ToString();

                    cid.GetUpgradeableProperty(CharacterUpgradableProperty.Str);
                    cid.GetUpgradeableProperty(CharacterUpgradableProperty.Vit);
                    cid.GetUpgradeableProperty(CharacterUpgradableProperty.Dex);
                    cid.GetUpgradeableProperty(CharacterUpgradableProperty.Spi);

                    StrTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Str].Value.ToString();
                    StrExtraTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Str].Extra.ToString();
                    VitTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Vit].Value.ToString();
                    VitExtraTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Vit].Extra.ToString();
                    DexTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Dex].Value.ToString();
                    DexExtraTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Dex].Extra.ToString();
                    SpiTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Spi].Value.ToString();
                    SpiExtraTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Spi].Extra.ToString();
                }

            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                GrowthFactorTextBox.Text = GrowthFactorTextBox.Tag.ToString();
            }
        }

        private void hpTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("hpTextBox_GotFocus");
            try
            {
                HpTextBox.Tag = HpTextBox.Text;
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void hpTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("hpTextBox_LostFocus");
            try
            {
                if (string.IsNullOrEmpty(HpTextBox.Text))
                {
                    HpTextBox.Text = "1";
                }

                int hp = Mathf.Clamp(int.Parse(HpTextBox.Text), 1, int.Parse(MaxHpTextBox.Text));

                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    cid.HP = hp;
                }
                HpTextBox.Text = hp.ToString();
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                HpTextBox.Text = HpTextBox.Tag.ToString();
            }
        }

        private void maxHpTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("maxHpTextBox_GotFocus");
            try
            {
                MaxHpTextBox.Tag = MaxHpTextBox.Text;
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void maxHpTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("maxHpTextBox_LostFocus");
            try
            {
                int maxHp = 1;

                if (string.IsNullOrEmpty(MaxHpTextBox.Text))
                {
                    maxHp = 1;
                }

                maxHp = Mathf.Clamp(int.Parse(MaxHpTextBox.Text), 1, int.MaxValue);

                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    cid.Property[CharacterProperty.Max_HP].Base = maxHp - cid.Property[CharacterProperty.Max_HP].Equip_Attach - cid.Property[CharacterProperty.Max_HP].Four_Attribute_Attach;

                    int hp = Mathf.Clamp(cid.HP, 1, maxHp);
                    cid.HP = hp;

                    MaxHpTextBox.Text = maxHp.ToString();
                    HpTextBox.Text = hp.ToString();
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                MaxHpTextBox.Text = MaxHpTextBox.Tag.ToString();
            }
        }

        private void AffiliationTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("AffiliationTextBox_GotFocus");
            try
            {
                AffiliationTextBox.Tag = AffiliationTextBox.Text;
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void AffiliationTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("AffiliationTextBox_LostFocus");
            try
            {
                if (string.IsNullOrEmpty(AffiliationTextBox.Text))
                {
                    AffiliationTextBox.Text = "0";
                }

                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    int affiliation = int.Parse(AffiliationTextBox.Text);

                    cid.Property[CharacterProperty.Affiliation].Base = affiliation;

                    AffiliationTextBox.Text = affiliation.ToString();

                    if (cid.Property[CharacterProperty.Affiliation].Value > 0)
                    {
                        AffiliationStrTextBox.Text = "楚天碧";
                    }
                    else if (cid.Property[CharacterProperty.Affiliation].Value < 0)
                    {
                        AffiliationStrTextBox.Text = "段霄烈";
                    }
                    else
                    {
                        AffiliationStrTextBox.Text = "无";
                    }
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                AffiliationTextBox.Text = AffiliationTextBox.Tag.ToString();
            }
        }

        private void mpTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("mpTextBox_GotFocus");
            try
            {
                MpTextBox.Tag = MpTextBox.Text;
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void mpTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("mpTextBox_LostFocus");
            try
            {
                if (string.IsNullOrEmpty(MpTextBox.Text))
                {
                    MpTextBox.Text = "1";
                }

                int mp = Mathf.Clamp(int.Parse(MpTextBox.Text), 1, int.Parse(MaxMpTextBox.Text));

                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    cid.MP = mp;
                }
                MpTextBox.Text = mp.ToString();
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                MpTextBox.Text = MpTextBox.Tag.ToString();
            }
        }

        private void maxMpTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("maxMpTextBox_GotFocus");
            MaxMpTextBox.Tag = MaxMpTextBox.Text;
        }

        private void maxMpTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("maxMpTextBox_LostFocus");
            try
            {
                if (string.IsNullOrEmpty(MaxMpTextBox.Text))
                {
                    MaxMpTextBox.Text = "1";
                }

                int maxMp = Mathf.Clamp(int.Parse(MaxMpTextBox.Text), 1, int.MaxValue);

                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    cid.Property[CharacterProperty.Max_MP].Base = maxMp - cid.Property[CharacterProperty.Max_MP].Equip_Attach - cid.Property[CharacterProperty.Max_MP].Four_Attribute_Attach;

                    int mp = Mathf.Clamp(cid.MP, 1, maxMp);
                    cid.MP = mp;

                    MaxMpTextBox.Text = maxMp.ToString();
                    MpTextBox.Text = mp.ToString();
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                MaxMpTextBox.Text = MaxMpTextBox.Tag.ToString();
            }
        }

        private void strLevelTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("strLevelTextBox_GotFocus");
            StrLevelTextBox.Tag = StrLevelTextBox.Text;
        }

        private void strLevelTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("strLevelTextBox_LostFocus");
            try
            {
                if (string.IsNullOrEmpty(StrLevelTextBox.Text))
                {
                    StrLevelTextBox.Text = "0";
                }

                int str = Mathf.Clamp(int.Parse(StrLevelTextBox.Text), 1, int.MaxValue);

                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    cid.UpgradeableProperty[CharacterUpgradableProperty.Str].Level = str;

                    cid.GetUpgradeableProperty(CharacterUpgradableProperty.Str);
                    StrTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Str].Value.ToString();
                    StrLevelTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Str].Level.ToString();
                    StrExtraTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Str].Extra.ToString();

                    //createFormula(cid);
                    cid.UpgradeProperty(false);
                    readCharacterProperty(cid);
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                StrLevelTextBox.Text = StrLevelTextBox.Tag.ToString();
            }
        }

        private void vitLevelTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("vitLevelTextBox_GotFocus");
            VitLevelTextBox.Tag = VitLevelTextBox.Text;
        }

        private void vitLevelTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("vitLevelTextBox_LostFocus");
            try
            {
                if (string.IsNullOrEmpty(VitLevelTextBox.Text))
                {
                    VitLevelTextBox.Text = "0";
                }

                int vit = Mathf.Clamp(int.Parse(VitLevelTextBox.Text), 1, int.MaxValue);

                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    cid.UpgradeableProperty[CharacterUpgradableProperty.Vit].Level = vit;

                    cid.GetUpgradeableProperty(CharacterUpgradableProperty.Vit);
                    VitTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Vit].Value.ToString();
                    VitLevelTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Vit].Level.ToString();
                    VitExtraTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Vit].Extra.ToString();

                    //createFormula(cid);
                    cid.UpgradeProperty(false);
                    readCharacterProperty(cid);
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                VitLevelTextBox.Text = VitLevelTextBox.Tag.ToString();
            }
        }

        private void dexLevelTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("dexLevelTextBox_GotFocus");
            DexLevelTextBox.Tag = DexLevelTextBox.Text;
        }

        private void dexLevelTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("dexLevelTextBox_LostFocus");
            try
            {
                if (string.IsNullOrEmpty(DexLevelTextBox.Text))
                {
                    DexLevelTextBox.Text = "0";
                }

                int dex = Mathf.Clamp(int.Parse(DexLevelTextBox.Text), 1, int.MaxValue);

                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    cid.UpgradeableProperty[CharacterUpgradableProperty.Dex].Level = dex;

                    cid.GetUpgradeableProperty(CharacterUpgradableProperty.Dex);
                    DexTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Dex].Value.ToString();
                    DexLevelTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Dex].Level.ToString();
                    DexExtraTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Dex].Extra.ToString();

                    //createFormula(cid);
                    cid.UpgradeProperty(false);
                    readCharacterProperty(cid);
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                DexLevelTextBox.Text = DexLevelTextBox.Tag.ToString();
            }
        }

        private void spiLevelTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("spiLevelTextBox_GotFocus");
            SpiLevelTextBox.Tag = SpiLevelTextBox.Text;
        }

        private void spiLevelTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("spiLevelTextBox_LostFocus");
            try
            {
                if (string.IsNullOrEmpty(SpiLevelTextBox.Text))
                {
                    SpiLevelTextBox.Text = "0";
                }

                int spi = Mathf.Clamp(int.Parse(SpiLevelTextBox.Text), 1, int.MaxValue);

                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    cid.UpgradeableProperty[CharacterUpgradableProperty.Spi].Level = spi;

                    cid.GetUpgradeableProperty(CharacterUpgradableProperty.Spi);
                    SpiTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Spi].Value.ToString();
                    SpiLevelTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Spi].Level.ToString();
                    SpiExtraTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Spi].Extra.ToString();

                    //createFormula(cid);
                    cid.UpgradeProperty(false);
                    readCharacterProperty(cid);
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                SpiLevelTextBox.Text = SpiLevelTextBox.Tag.ToString();
            }
        }

        private void attackTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("attackTextBox_GotFocus");
            AttackTextBox.Tag = AttackTextBox.Text;
        }

        private void attackTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("attackTextBox_LostFocus");
            try
            {
                if (string.IsNullOrEmpty(AttackTextBox.Text))
                {
                    AttackTextBox.Text = "0";
                }

                int attack = Mathf.Clamp(int.Parse(AttackTextBox.Text), 1, int.MaxValue);

                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    cid.Property[CharacterProperty.Attack].Base = attack - cid.Property[CharacterProperty.Attack].Equip_Attach - cid.Property[CharacterProperty.Attack].Four_Attribute_Attach;

                    AttackTextBox.Text = attack.ToString();
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                AttackTextBox.Text = AttackTextBox.Tag.ToString();
            }
        }

        private void defenseTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("defenseTextBox_GotFocus");
            DefenseTextBox.Tag = DefenseTextBox.Text;
        }

        private void defenseTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("defenseTextBox_LostFocus");
            try
            {
                if (string.IsNullOrEmpty(DefenseTextBox.Text))
                {
                    DefenseTextBox.Text = "0";
                }

                int defense = Mathf.Clamp(int.Parse(DefenseTextBox.Text), 1, int.MaxValue);

                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    cid.Property[CharacterProperty.Defense].Base = defense - cid.Property[CharacterProperty.Defense].Equip_Attach - cid.Property[CharacterProperty.Defense].Four_Attribute_Attach;

                    DefenseTextBox.Text = defense.ToString();
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                DefenseTextBox.Text = DefenseTextBox.Tag.ToString();
            }
        }

        private void hitTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("hitTextBox_GotFocus");
            HitTextBox.Tag = HitTextBox.Text;
        }

        private void hitTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("hitTextBox_LostFocus");
            try
            {
                if (string.IsNullOrEmpty(HitTextBox.Text))
                {
                    HitTextBox.Text = "0";
                }

                int hit = Mathf.Clamp(int.Parse(HitTextBox.Text), 1, int.MaxValue);

                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    cid.Property[CharacterProperty.Hit].Base = hit - cid.Property[CharacterProperty.Hit].Equip_Attach - cid.Property[CharacterProperty.Hit].Four_Attribute_Attach;

                    HitTextBox.Text = hit.ToString();
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                HitTextBox.Text = HitTextBox.Tag.ToString();
            }
        }

        private void moveTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("moveTextBox_GotFocus");
            MoveTextBox.Tag = MoveTextBox.Text;
        }

        private void moveTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("moveTextBox_LostFocus");
            try
            {
                if (string.IsNullOrEmpty(MoveTextBox.Text))
                {
                    MoveTextBox.Text = "0";
                }

                int move = Mathf.Clamp(int.Parse(MoveTextBox.Text), 1, int.MaxValue);

                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    cid.Property[CharacterProperty.Move].Base = move - cid.Property[CharacterProperty.Move].Equip_Attach - cid.Property[CharacterProperty.Move].Four_Attribute_Attach;

                    MoveTextBox.Text = move.ToString();
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                MoveTextBox.Text = MoveTextBox.Tag.ToString();
            }
        }

        private void dodgeTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("dodgeTextBox_GotFocus");
            DodgeTextBox.Tag = DodgeTextBox.Text;
        }

        private void dodgeTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("dodgeTextBox_LostFocus");
            try
            {
                if (string.IsNullOrEmpty(DodgeTextBox.Text))
                {
                    DodgeTextBox.Text = "0";
                }

                int dodge = Mathf.Clamp(int.Parse(DodgeTextBox.Text), 1, int.MaxValue);

                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    cid.Property[CharacterProperty.Dodge].Base = dodge - cid.Property[CharacterProperty.Dodge].Equip_Attach - cid.Property[CharacterProperty.Dodge].Four_Attribute_Attach;

                    DodgeTextBox.Text = dodge.ToString();
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                DodgeTextBox.Text = DodgeTextBox.Tag.ToString();
            }
        }

        private void parryTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("parryTextBox_GotFocus");
            ParryTextBox.Tag = ParryTextBox.Text;
        }

        private void parryTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("parryTextBox_LostFocus");
            try
            {
                if (string.IsNullOrEmpty(ParryTextBox.Text))
                {
                    ParryTextBox.Text = "0";
                }

                int parry = Mathf.Clamp(int.Parse(ParryTextBox.Text), 1, int.MaxValue);

                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    cid.Property[CharacterProperty.Parry].Base = parry - cid.Property[CharacterProperty.Parry].Equip_Attach - cid.Property[CharacterProperty.Parry].Four_Attribute_Attach;

                    ParryTextBox.Text = parry.ToString();
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                ParryTextBox.Text = ParryTextBox.Tag.ToString();
            }
        }

        private void criticalTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("criticalTextBox_GotFocus");
            CriticalTextBox.Tag = CriticalTextBox.Text;
        }

        private void criticalTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("criticalTextBox_LostFocus");
            try
            {
                if (string.IsNullOrEmpty(CriticalTextBox.Text))
                {
                    CriticalTextBox.Text = "0";
                }

                int critical = Mathf.Clamp(int.Parse(CriticalTextBox.Text), 1, int.MaxValue);

                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    cid.Property[CharacterProperty.Critical].Base = critical - cid.Property[CharacterProperty.Critical].Equip_Attach - cid.Property[CharacterProperty.Critical].Four_Attribute_Attach;

                    CriticalTextBox.Text = critical.ToString();
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                CriticalTextBox.Text = CriticalTextBox.Tag.ToString();
            }
        }

        private void counterTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("counterTextBox_GotFocus");
            CounterTextBox.Tag = CounterTextBox.Text;
        }

        private void counterTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("counterTextBox_LostFocus");
            try
            {
                if (string.IsNullOrEmpty(CounterTextBox.Text))
                {
                    CounterTextBox.Text = "0";
                }

                int counter = Mathf.Clamp(int.Parse(CounterTextBox.Text), 1, int.MaxValue);

                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    cid.Property[CharacterProperty.Counter].Base = counter - cid.Property[CharacterProperty.Counter].Equip_Attach - cid.Property[CharacterProperty.Counter].Four_Attribute_Attach;

                    CounterTextBox.Text = counter.ToString();
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                CounterTextBox.Text = CounterTextBox.Tag.ToString();
            }
        }

        private void VibrantTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("VibrantTextBox_GotFocus");
            VibrantTextBox.Tag = VibrantTextBox.Text;
        }

        private void VibrantTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("VibrantTextBox_LostFocus");
            try
            {
                if (string.IsNullOrEmpty(VibrantTextBox.Text))
                {
                    VibrantTextBox.Text = "0";
                }

                int vibrant = Mathf.Clamp(int.Parse(VibrantTextBox.Text), 0, int.MaxValue);

                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    cid.UpgradeableProperty[CharacterUpgradableProperty.Vibrant].Level = vibrant;

                    VibrantTextBox.Text = vibrant.ToString();
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                VibrantTextBox.Text = VibrantTextBox.Tag.ToString();
            }
        }

        private void CultivatedTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("CultivatedTextBox_GotFocus");
            CultivatedTextBox.Tag = CultivatedTextBox.Text;
        }

        private void CultivatedTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("CultivatedTextBox_LostFocus");
            try
            {
                if (string.IsNullOrEmpty(CultivatedTextBox.Text))
                {
                    CultivatedTextBox.Text = "0";
                }

                int Cultivated = Mathf.Clamp(int.Parse(CultivatedTextBox.Text), 0, int.MaxValue);

                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    cid.UpgradeableProperty[CharacterUpgradableProperty.Cultivated].Level = Cultivated;

                    CultivatedTextBox.Text = Cultivated.ToString();
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                CultivatedTextBox.Text = CultivatedTextBox.Tag.ToString();
            }
        }

        private void ResoluteTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("ResoluteTextBox_GotFocus");
            ResoluteTextBox.Tag = ResoluteTextBox.Text;
        }

        private void ResoluteTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("ResoluteTextBox_LostFocus");
            try
            {
                if (string.IsNullOrEmpty(ResoluteTextBox.Text))
                {
                    ResoluteTextBox.Text = "0";
                }

                int Resolute = Mathf.Clamp(int.Parse(ResoluteTextBox.Text), 0, int.MaxValue);

                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    cid.UpgradeableProperty[CharacterUpgradableProperty.Resolute].Level = Resolute;

                    ResoluteTextBox.Text = Resolute.ToString();
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                ResoluteTextBox.Text = ResoluteTextBox.Tag.ToString();
            }
        }

        private void BraveTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("BraveTextBox_GotFocus");
            BraveTextBox.Tag = BraveTextBox.Text;
        }

        private void BraveTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("BraveTextBox_LostFocus");
            try
            {
                if (string.IsNullOrEmpty(BraveTextBox.Text))
                {
                    BraveTextBox.Text = "0";
                }

                int Brave = Mathf.Clamp(int.Parse(BraveTextBox.Text), 0, int.MaxValue);

                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    cid.UpgradeableProperty[CharacterUpgradableProperty.Brave].Level = Brave;

                    BraveTextBox.Text = Brave.ToString();
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                BraveTextBox.Text = BraveTextBox.Tag.ToString();
            }
        }

        private void ZitherTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("ZitherTextBox_GotFocus");
            ZitherTextBox.Tag = ZitherTextBox.Text;
        }

        private void ZitherTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("ZitherTextBox_LostFocus");
            try
            {
                if (string.IsNullOrEmpty(ZitherTextBox.Text))
                {
                    ZitherTextBox.Text = "0";
                }

                int Zither = Mathf.Clamp(int.Parse(ZitherTextBox.Text), 0, int.MaxValue);

                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    cid.UpgradeableProperty[CharacterUpgradableProperty.Zither].Level = Zither;

                    ZitherTextBox.Text = Zither.ToString();
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                ZitherTextBox.Text = ZitherTextBox.Tag.ToString();
            }
        }

        private void ChessTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("ChessTextBox_GotFocus");
            ChessTextBox.Tag = ChessTextBox.Text;
        }

        private void ChessTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("ChessTextBox_LostFocus");
            try
            {
                if (string.IsNullOrEmpty(ChessTextBox.Text))
                {
                    ChessTextBox.Text = "0";
                }

                int Chess = Mathf.Clamp(int.Parse(ChessTextBox.Text), 0, int.MaxValue);

                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    cid.UpgradeableProperty[CharacterUpgradableProperty.Chess].Level = Chess;

                    ChessTextBox.Text = Chess.ToString();
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                ChessTextBox.Text = ChessTextBox.Tag.ToString();
            }
        }

        private void CalligraphyTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("CalligraphyTextBox_GotFocus");
            CalligraphyTextBox.Tag = CalligraphyTextBox.Text;
        }

        private void CalligraphyTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("CalligraphyTextBox_LostFocus");
            try
            {
                if (string.IsNullOrEmpty(CalligraphyTextBox.Text))
                {
                    CalligraphyTextBox.Text = "0";
                }

                int Calligraphy = Mathf.Clamp(int.Parse(CalligraphyTextBox.Text), 0, int.MaxValue);

                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    cid.UpgradeableProperty[CharacterUpgradableProperty.Calligraphy].Level = Calligraphy;

                    CalligraphyTextBox.Text = Calligraphy.ToString();
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                CalligraphyTextBox.Text = CalligraphyTextBox.Tag.ToString();
            }
        }

        private void PaintingTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("PaintingTextBox_GotFocus");
            PaintingTextBox.Tag = PaintingTextBox.Text;
        }

        private void PaintingTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("PaintingTextBox_LostFocus");
            try
            {
                if (string.IsNullOrEmpty(PaintingTextBox.Text))
                {
                    PaintingTextBox.Text = "0";
                }

                int Painting = Mathf.Clamp(int.Parse(PaintingTextBox.Text), 0, int.MaxValue);

                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    cid.UpgradeableProperty[CharacterUpgradableProperty.Painting].Level = Painting;

                    PaintingTextBox.Text = Painting.ToString();
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                PaintingTextBox.Text = PaintingTextBox.Tag.ToString();
            }
        }

        private void skillListView_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("skillListView_GotFocus");
            LearnSkillButton.Enabled = true;
            AbolishSkillButton.Enabled = false;
            SetSkill1Button.Enabled = false;
            SetSkill2Button.Enabled = false;
            SetSkill3Button.Enabled = false;
            SetSkill4Button.Enabled = false;
            SkillCurrentLevelTextBox.Enabled = false;
            SkillMaxLevelTextBox.Enabled = false;
        }

        private void havingSkillListView_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("havingSkillListView_GotFocus");
            LearnSkillButton.Enabled = false;
            AbolishSkillButton.Enabled = true;
            SetSkill1Button.Enabled = true;
            SetSkill2Button.Enabled = true;
            SetSkill3Button.Enabled = true;
            SetSkill4Button.Enabled = true;
            SkillCurrentLevelTextBox.Enabled = true;
            SkillMaxLevelTextBox.Enabled = true;
        }

        private void learnSkillButton_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("learnSkillButton_Click");
            try
            {
                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    foreach (ListViewItem skillLvi in SkillListView.SelectedItems)
                    {
                        if (!string.IsNullOrEmpty(skillLvi.Text))
                        {
                            cid.LearnSkill(skillLvi.Text);
                        }
                    }
                    readCharacterSkillData(cid);
                    readCharacterEquipSkillData(cid);
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void abolishSkillButton_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("abolishSkillButton_Click");
            try
            {
                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    int index = -1;
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    foreach (ListViewItem skillLvi in HavingSkillListView.SelectedItems)
                    {
                        index = skillLvi.Index;
                        cid.AbolishSkill(skillLvi.Text);
                        cid.EquipSkills.ReplaceEquipSkill(skillLvi.Text, "", cid.IsPlayer);
                    }
                    readCharacterSkillData(cid);
                    readCharacterEquipSkillData(cid);

                    if (index == HavingSkillListView.Items.Count)
                    {
                        index--;
                    }
                    if (index != -1)
                    {
                        HavingSkillListView.Items[index].Selected = true;
                    }
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void havingSkillListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            LogHelper.Debug("havingSkillListView_SelectedIndexChanged");
            try
            {
                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    foreach (ListViewItem skillLvi in HavingSkillListView.SelectedItems)
                    {
                        SkillData sd = cid.Skill[skillLvi.Text];

                        SkillCurrentLevelTextBox.Text = sd.Level.ToString();
                        SkillMaxLevelTextBox.Text = sd.MaxLevel.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void setSkill1Button_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("setSkill1Button_Click");
            try
            {
                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    Props weapon = cid.Equip.GetEquip(EquipType.Weapon);
                    if (weapon == null)
                    {
                        if (MessageBox.Show("未选择武器的情况下，将默认设定为拳法的技能", "", MessageBoxButtons.OKCancel) == DialogResult.OK)
                        {
                            foreach (ListViewItem skillLvi in HavingSkillListView.SelectedItems)
                            {
                                cid.SetEquipSkill(SkillColumn.Skill01, skillLvi.Text);
                                readCharacterEquipSkillData(cid);
                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void setSkill2Button_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("setSkill2Button_Click");
            try
            {
                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    Props weapon = cid.Equip.GetEquip(EquipType.Weapon);
                    if (weapon == null)
                    {
                        if (MessageBox.Show("未选择武器的情况下，将默认设定为拳法的技能", "", MessageBoxButtons.OKCancel) == DialogResult.OK)
                        {
                            foreach (ListViewItem skillLvi in HavingSkillListView.SelectedItems)
                            {
                                cid.SetEquipSkill(SkillColumn.Skill02, skillLvi.Text);
                                readCharacterEquipSkillData(cid);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void setSkill3Button_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("setSkill3Button_Click");
            try
            {
                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    Props weapon = cid.Equip.GetEquip(EquipType.Weapon);
                    if (weapon == null)
                    {
                        if (MessageBox.Show("未选择武器的情况下，将默认设定为拳法的技能", "", MessageBoxButtons.OKCancel) == DialogResult.OK)
                        {
                            foreach (ListViewItem skillLvi in HavingSkillListView.SelectedItems)
                            {
                                cid.SetEquipSkill(SkillColumn.Skill03, skillLvi.Text);
                                readCharacterEquipSkillData(cid);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void setSkill4Button_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("setSkill4Button_Click");
            try
            {
                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    Props weapon = cid.Equip.GetEquip(EquipType.Weapon);
                    if (weapon == null)
                    {
                        if (MessageBox.Show("未选择武器的情况下，将默认设定为拳法的技能", "", MessageBoxButtons.OKCancel) == DialogResult.OK)
                        {
                            foreach (ListViewItem skillLvi in HavingSkillListView.SelectedItems)
                            {
                                cid.SetEquipSkill(SkillColumn.Skill04, skillLvi.Text);
                                readCharacterEquipSkillData(cid);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }
        private void skillCurrentLevelTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("skillCurrentLevelTextBox_GotFocus");
            SkillCurrentLevelTextBox.Tag = SkillCurrentLevelTextBox.Text;
        }

        private void skillCurrentLevelTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("skillCurrentLevelTextBox_LostFocus");
            try
            {
                if (string.IsNullOrEmpty(SkillCurrentLevelTextBox.Text))
                {
                    SkillCurrentLevelTextBox.Text = "1";
                }
                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    foreach (ListViewItem skillLvi in HavingSkillListView.SelectedItems)
                    {
                        int level = Mathf.Clamp(int.Parse(SkillCurrentLevelTextBox.Text), 1, int.MaxValue);

                        cid.GetSkill(skillLvi.Text).Level = level;

                        SkillCurrentLevelTextBox.Text = cid.GetSkill(skillLvi.Text).Level.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                SkillCurrentLevelTextBox.Text = SkillCurrentLevelTextBox.Tag.ToString();
            }
        }

        private void skillMaxLevelTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("skillMaxLevelTextBox_GotFocus");
            SkillMaxLevelTextBox.Tag = SkillMaxLevelTextBox.Text;
        }

        private void skillMaxLevelTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("skillMaxLevelTextBox_LostFocus");
            try
            {
                if (string.IsNullOrEmpty(SkillMaxLevelTextBox.Text))
                {
                    SkillMaxLevelTextBox.Text = "1";
                }
                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    foreach (ListViewItem skillLvi in HavingSkillListView.SelectedItems)
                    {
                        int MaxLevel = Mathf.Clamp(int.Parse(SkillMaxLevelTextBox.Text), 1, int.MaxValue);

                        cid.GetSkill(skillLvi.Text).MaxLevel = MaxLevel;

                        SkillMaxLevelTextBox.Text = cid.GetSkill(skillLvi.Text).MaxLevel.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                SkillMaxLevelTextBox.Text = SkillMaxLevelTextBox.Tag.ToString();
            }
        }

        private void AddTraitButton_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("AddTraitButton_Click");
            try
            {
                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    foreach (ListViewItem traitLvi in TraitListView.SelectedItems)
                    {
                        cid.LearnTrait(traitLvi.Text);
                    }
                    readCharacterTraitData(cid);
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void AbolishTraitButton_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("AbolishTraitButton_Click");
            try
            {
                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    int index = -1;
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    foreach (ListViewItem traitLvi in HavingTraitListView.SelectedItems)
                    {
                        index = traitLvi.Index;
                        cid.AbolishTrait(traitLvi.Text);
                    }
                    readCharacterTraitData(cid);

                    if (index == HavingTraitListView.Items.Count)
                    {
                        index--;
                    }
                    if (index != -1)
                    {
                        HavingTraitListView.Items[index].Selected = true;
                    }
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        public void readCharacterTraitData(CharacterInfoData cid)
        {
            LogHelper.Debug("readCharacterTraitData");
            try
            {
                HavingTraitListView.Items.Clear();
                foreach (KeyValuePair<string, TraitData> kv in cid.Trait)
                {
                    if (!string.IsNullOrEmpty(kv.Key))
                    {
                        ListViewItem lvi = new ListViewItem();

                        lvi.Text = kv.Key;

                        lvi.SubItems.Add(kv.Value.Item.Name);

                        HavingTraitListView.Items.Add(lvi);
                    }
                }

                HavingTraitListView.EndUpdate();  //结束数据处理，UI界面一次性绘制。 
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void TraitListView_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("TraitListView_GotFocus");
            AddTraitButton.Enabled = true;
            AbolishTraitButton.Enabled = false;
        }

        private void HavingTraitListView_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("HavingTraitListView_GotFocus");
            AddTraitButton.Enabled = false;
            AbolishTraitButton.Enabled = true;
        }
        public void readCharacterMantraData(CharacterInfoData cid)
        {
            LogHelper.Debug("readCharacterMantraData");
            try
            {
                HavingMantraListView.Items.Clear();
                foreach (KeyValuePair<string, MantraData> kv in cid.Mantra)
                {
                    if (!string.IsNullOrEmpty(kv.Key))
                    {
                        ListViewItem lvi = new ListViewItem();

                        lvi.Text = kv.Key;

                        lvi.SubItems.Add(kv.Value.Item.Name);

                        HavingMantraListView.Items.Add(lvi);
                    }
                }

                HavingMantraListView.EndUpdate();  //结束数据处理，UI界面一次性绘制。 
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }
        public void readCharacterWorkMantraData(CharacterInfoData cid)
        {
            LogHelper.Debug("readCharacterWorkMantraData");
            try
            {
                string WorkMantra = cid.WorkMantra;
                WorkMantraLabel.Text = string.IsNullOrEmpty(WorkMantra) ? "无" : Data.Get<Mantra>(WorkMantra).Name;
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void MantraListView_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("MantraListView_GotFocus");
            LearnMantraButton.Enabled = true;
            AbolishMantraButton.Enabled = false;
        }

        private void HavingMantraListView_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("HavingMantraListView_GotFocus");
            LearnMantraButton.Enabled = false;
            AbolishMantraButton.Enabled = true;
        }

        private void LearnMantraButton_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("LearnMantraButton_Click");
            try
            {
                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    foreach (ListViewItem mantraLvi in MantraListView.SelectedItems)
                    {
                        cid.LearnMantra(mantraLvi.Text);
                    }
                    readCharacterMantraData(cid);
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void AbolishMantraButton_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("AbolishMantraButton_Click");
            try
            {
                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    int index = -1;
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    foreach (ListViewItem mantraLvi in HavingMantraListView.SelectedItems)
                    {
                        index = mantraLvi.Index;
                        cid.AbolishMantra(mantraLvi.Text);
                        if (cid.WorkMantra == mantraLvi.Text)
                        {
                            cid.WorkMantra = null;
                        }
                    }
                    readCharacterMantraData(cid);
                    readCharacterWorkMantraData(cid);

                    if (index == HavingMantraListView.Items.Count)
                    {
                        index--;
                    }
                    if (index != -1)
                    {
                        HavingMantraListView.Items[index].Selected = true;
                    }
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void SetWorkMantraButton_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("SetWorkMantraButton_Click");
            try
            {
                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    foreach (ListViewItem mantraLvi in HavingMantraListView.SelectedItems)
                    {
                        cid.WorkMantra = mantraLvi.Text;
                        readCharacterWorkMantraData(cid);
                    }

                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void HavingMantraListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            LogHelper.Debug("HavingMantraListView_SelectedIndexChanged");
            try
            {
                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    foreach (ListViewItem mantraLvi in HavingMantraListView.SelectedItems)
                    {
                        MantraData md = cid.Mantra[mantraLvi.Text];

                        MantraCurrentLevelTextBox.Text = md.Level.ToString();
                        MantraMaxLevelTextBox.Text = md.MaxLevel.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void MantraCurrentLevelTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("MantraCurrentLevelTextBox_GotFocus");
            MantraCurrentLevelTextBox.Tag = MantraCurrentLevelTextBox.Text;
        }

        private void MantraCurrentLevelTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("MantraCurrentLevelTextBox_LostFocus");
            try
            {
                if (string.IsNullOrEmpty(MantraCurrentLevelTextBox.Text))
                {
                    MantraCurrentLevelTextBox.Text = "1";
                }
                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    foreach (ListViewItem mantraLvi in HavingMantraListView.SelectedItems)
                    {
                        int level = Mathf.Clamp(int.Parse(MantraCurrentLevelTextBox.Text), 1, int.MaxValue);
                        cid.GetMantra(mantraLvi.Text).Level = level;

                        MantraCurrentLevelTextBox.Text = cid.GetMantra(mantraLvi.Text).Level.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                MantraCurrentLevelTextBox.Text = MantraCurrentLevelTextBox.Tag.ToString();
            }
        }

        private void MantraMaxLevelTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("MantraMaxLevelTextBox_GotFocus");
            MantraMaxLevelTextBox.Tag = MantraMaxLevelTextBox.Text;
        }

        private void MantraMaxLevelTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("MantraMaxLevelTextBox_LostFocus");
            try
            {
                if (string.IsNullOrEmpty(MantraMaxLevelTextBox.Text))
                {
                    MantraMaxLevelTextBox.Text = "1";
                }
                foreach (ListViewItem lvi in CharacterListView.SelectedItems)
                {
                    CharacterInfoData cid = gameData.Character[lvi.Text];

                    foreach (ListViewItem mantraLvi in HavingMantraListView.SelectedItems)
                    {
                        int MaxLevel = Mathf.Clamp(int.Parse(MantraMaxLevelTextBox.Text), 1, int.MaxValue);
                        cid.GetMantra(mantraLvi.Text).MaxLevel = MaxLevel;

                        MantraMaxLevelTextBox.Text = cid.GetMantra(mantraLvi.Text).MaxLevel.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                MantraMaxLevelTextBox.Text = MantraMaxLevelTextBox.Tag.ToString();
            }
        }

        private void characterExteriorListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            LogHelper.Debug("characterExteriorListView_SelectedIndexChanged");
            messageLabel.Text = "";
            try
            {
                foreach (ListViewItem lvi in CharacterExteriorListView.SelectedItems)
                {  //选中项遍历 
                    if (!saveFileIsSelected)
                    {
                        messageLabel.Text = "请先选择一个存档";
                        LogHelper.Debug("请先选择一个存档");
                        CharacterExteriorListView.SelectedItems.Clear();
                        return;
                    }
                    string id = lvi.Text;
                    if (id == "in0101")
                    {
                        id = "Player";
                    }
                    CharacterExteriorData ced = new CharacterExteriorData();
                    if (!gameData.Exterior.ContainsKey(id))
                    {
                        CharacterExterior characterExterior = Data.Get<CharacterExterior>(id);
                        if (characterExterior != null)
                        {
                            ced = new CharacterExteriorData(characterExterior);
                            gameData.Exterior.Add(id, ced);
                        }
                    }
                    else
                    {
                        ced = gameData.Exterior[id];
                    }
                    readSelectCharacterExteriorData(ced);
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        public void readSelectCharacterExteriorData(CharacterExteriorData ced)
        {
            LogHelper.Debug("readSelectCharacterExteriorData");
            try
            {
                SurNameTextBox.Text = ced.SurName;
                NameTextBox.Text = ced.Name;
                NicknameTextBox.Text = ced.Nickname;
                ProtraitTextBox.Text = ced.Protrait;
                ModelTextBox.Text = ced.Model;
                GenderComboBox.SelectedIndex = (int)ced.Gender;
                DescriptionTextBox.Text = ced.Description;
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void SurNameTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("SurNameTextBox_GotFocus");
            SurNameTextBox.Tag = SurNameTextBox.Text;
        }

        private void SurNameTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("SurNameTextBox_LostFocus");
            messageLabel.Text = "";
            try
            {

                foreach (ListViewItem lvi in CharacterExteriorListView.SelectedItems)
                {
                    CharacterExteriorData ced = gameData.Exterior[lvi.Text];

                    ced.SurName = SurNameTextBox.Text;
                    SurNameTextBox.Text = ced.SurName;
                }

                readCommunity();
                readExteriorName();
                readParty();
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                SurNameTextBox.Text = SurNameTextBox.Tag.ToString();
            }
        }

        private void NameTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("NameTextBox_GotFocus");
            NameTextBox.Tag = NameTextBox.Text;
        }

        private void NameTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("NameTextBox_LostFocus");
            messageLabel.Text = "";
            try
            {

                foreach (ListViewItem lvi in CharacterExteriorListView.SelectedItems)
                {
                    CharacterExteriorData ced = gameData.Exterior[lvi.Text];

                    ced.Name = NameTextBox.Text;
                    NameTextBox.Text = ced.Name;
                }
                readCommunity();
                readExteriorName();
                readParty();
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                NameTextBox.Text = NameTextBox.Tag.ToString();
            }
        }

        private void NicknameTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("NicknameTextBox_GotFocus");
            NicknameTextBox.Tag = NicknameTextBox.Text;
        }

        private void NicknameTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("NicknameTextBox_LostFocus");
            messageLabel.Text = "";
            try
            {

                foreach (ListViewItem lvi in CharacterExteriorListView.SelectedItems)
                {
                    CharacterExteriorData ced = gameData.Exterior[lvi.Text];

                    ced.Nickname = NicknameTextBox.Text;
                    NicknameTextBox.Text = ced.Nickname;
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                NicknameTextBox.Text = NicknameTextBox.Tag.ToString();
            }
        }

        private void ProtraitTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("ProtraitTextBox_GotFocus");
            ProtraitTextBox.Tag = ProtraitTextBox.Text;
        }

        private void ProtraitTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("ProtraitTextBox_LostFocus");
            messageLabel.Text = "";
            try
            {

                foreach (ListViewItem lvi in CharacterExteriorListView.SelectedItems)
                {
                    CharacterExteriorData ced = gameData.Exterior[lvi.Text];

                    bool hasProtrait = false;
                    foreach (KeyValuePair<string, CharacterExterior> kv in Data.Get<CharacterExterior>())
                    {
                        if (kv.Value.Protrait == ProtraitTextBox.Text)
                        {

                            ced.Protrait = ProtraitTextBox.Text;
                            ProtraitTextBox.Text = ced.Protrait;
                            hasProtrait = true;
                            break;
                        }
                    }
                    if (!hasProtrait)
                    {
                        messageLabel.Text = "未找到该头像编号";
                        LogHelper.Debug("未找到该头像编号");

                        ModelTextBox.Text = ModelTextBox.Tag.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                ProtraitTextBox.Text = ProtraitTextBox.Tag.ToString();
            }
        }

        private void ModelTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("ModelTextBox_GotFocus");
            ModelTextBox.Tag = ModelTextBox.Text;
        }

        private void ModelTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("ModelTextBox_LostFocus");
            messageLabel.Text = "";
            if (string.IsNullOrEmpty(ModelTextBox.Text))
            {
                messageLabel.Text = "模型编号不可为空";
                LogHelper.Debug("模型编号不可为空");

                ModelTextBox.Text = ModelTextBox.Tag.ToString();
                return;
            }
            try
            {

                foreach (ListViewItem lvi in CharacterExteriorListView.SelectedItems)
                {
                    CharacterExteriorData ced = gameData.Exterior[lvi.Text];

                    bool hasModel = false;
                    foreach (KeyValuePair<string, CharacterExterior> kv in Data.Get<CharacterExterior>())
                    {
                        if (kv.Value.Model == ModelTextBox.Text)
                        {

                            ced.Model = kv.Value.Model;
                            ModelTextBox.Text = ced.Model;

                            ced.Gender = kv.Value.Gender;
                            GenderComboBox.SelectedIndex = (int)ced.Gender;

                            ced.Size = kv.Value.Size;
                            hasModel = true;

                            if (lvi.Text == "Player")
                            {
                                string[] playerCharacters = new string[] { "in0196", "in0197", "in0101", "in0115" };
                                for (int i = 0; i < playerCharacters.Length; i++)
                                {
                                    CharacterExteriorData cedtemp = gameData.Exterior[playerCharacters[i]];
                                    cedtemp.Model = kv.Value.Model;
                                    cedtemp.Gender = kv.Value.Gender;
                                    cedtemp.Size = kv.Value.Size;
                                }
                            }
                            break;
                        }
                    }
                    if (!hasModel)
                    {
                        messageLabel.Text = "未找到该模型编号";
                        LogHelper.Debug("未找到该模型编号");

                        ModelTextBox.Text = ModelTextBox.Tag.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                ModelTextBox.Text = ModelTextBox.Tag.ToString();
            }
        }

        private void DescriptionTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("DescriptionTextBox_GotFocus");
            DescriptionTextBox.Tag = DescriptionTextBox.Text;
        }

        private void DescriptionTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("DescriptionTextBox_LostFocus");
            messageLabel.Text = "";
            try
            {

                foreach (ListViewItem lvi in CharacterExteriorListView.SelectedItems)
                {
                    CharacterExteriorData ced = gameData.Exterior[lvi.Text];

                    ced.Description = DescriptionTextBox.Text;
                    DescriptionTextBox.Text = ced.Description;
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                DescriptionTextBox.Text = DescriptionTextBox.Tag.ToString();
            }
        }

        private void CommunityListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            LogHelper.Debug("CommunityListView_SelectedIndexChanged");
            try
            {
                foreach (ListViewItem lvi in CommunityListView.SelectedItems)
                {
                    CommunityData cd = gameData.Community[lvi.Text];

                    CommunityLevelTextBox.Text = cd.Favorability.Level.ToString();
                    CommunityMaxLevelTextBox.Text = cd.Favorability.MaxLevel.ToString();
                    CommunityExpTextBox.Text = cd.Favorability.Exp.ToString();
                    CommunityIsOpenCheckBox.Checked = cd.isOpen;
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void CommunityListView_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("CommunityListView_GotFocus");
            AddPartyButton.Enabled = true;
            RemovePartyButton.Enabled = false;
        }

        private void PartyListView_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("PartyListView_GotFocus");
            AddPartyButton.Enabled = false;
            RemovePartyButton.Enabled = true;
        }

        private void CommunityMaxLevelTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("CommunityMaxLevelTextBox_GotFocus");
            CommunityMaxLevelTextBox.Tag = CommunityMaxLevelTextBox.Text;
        }

        private void CommunityMaxLevelTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("CommunityMaxLevelTextBox_LostFocus");
            try
            {
                if (string.IsNullOrEmpty(CommunityMaxLevelTextBox.Text))
                {
                    CommunityMaxLevelTextBox.Text = "1";
                }

                int maxLevel = Mathf.Clamp(int.Parse(CommunityMaxLevelTextBox.Text), 1, int.MaxValue);

                foreach (ListViewItem lvi in CommunityListView.SelectedItems)
                {
                    CommunityData cd = gameData.Community[lvi.Text];

                    cd.Favorability.MaxLevel = maxLevel;
                    CommunityMaxLevelTextBox.Text = cd.Favorability.MaxLevel.ToString();
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                CommunityMaxLevelTextBox.Text = CommunityMaxLevelTextBox.Tag.ToString();
            }
        }

        private void CommunityLevelTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("CommunityLevelTextBox_GotFocus");
            CommunityLevelTextBox.Tag = CommunityLevelTextBox.Text;
        }

        private void CommunityLevelTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("CommunityLevelTextBox_LostFocus");
            try
            {
                if (string.IsNullOrEmpty(CommunityLevelTextBox.Text))
                {
                    CommunityLevelTextBox.Text = "1";
                }

                int Level = Mathf.Clamp(int.Parse(CommunityLevelTextBox.Text), 1, int.MaxValue);

                foreach (ListViewItem lvi in CommunityListView.SelectedItems)
                {
                    CommunityData cd = gameData.Community[lvi.Text];

                    cd.Favorability.Level = Level;
                    CommunityLevelTextBox.Text = cd.Favorability.Level.ToString();

                    if (CharacterListView.SelectedItems[0].Text == lvi.Text)
                    {
                        CharacterInfoData cid = gameData.Character[lvi.Text];
                        if (cid.CommunityFormulaProperty == null)
                        {
                            cid.CommunityFormulaProperty = new Dictionary<string, int>
                            {
                                {
                                    "community_lv",
                                    Level
                                }
                            };
                        }
                        else if (cid.CommunityFormulaProperty.ContainsKey("community_lv"))
                        {
                            cid.CommunityFormulaProperty["community_lv"] = Level;
                        }
                        else
                        {
                            cid.CommunityFormulaProperty.Add("community_lv", Level);
                        }
                        if (cid.status_coefficient_of_community == null)
                        {
                            cid.status_coefficient_of_community = Game.Data.Get<GameFormula>("status_coefficient_of_community_" + cid.Id);
                        }
                        GrowthFactorTextBox.Text = (cid.status_coefficient_of_community.Evaluate(cid.CommunityFormulaProperty) + 1).ToString();

                        cid.GetUpgradeableProperty(CharacterUpgradableProperty.Str);
                        StrTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Str].Value.ToString();
                        StrExtraTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Str].Extra.ToString();

                        cid.GetUpgradeableProperty(CharacterUpgradableProperty.Vit);
                        VitTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Vit].Value.ToString();
                        VitExtraTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Vit].Extra.ToString();

                        cid.GetUpgradeableProperty(CharacterUpgradableProperty.Dex);
                        DexTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Dex].Value.ToString();
                        DexExtraTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Dex].Extra.ToString();

                        cid.GetUpgradeableProperty(CharacterUpgradableProperty.Spi);
                        SpiTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Spi].Value.ToString();
                        SpiExtraTextBox.Text = cid.UpgradeableProperty[CharacterUpgradableProperty.Spi].Extra.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                CommunityLevelTextBox.Text = CommunityLevelTextBox.Tag.ToString();
            }
        }

        private void CommunityExpTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("CommunityExpTextBox_GotFocus");
            CommunityExpTextBox.Tag = CommunityExpTextBox.Text;
        }

        private void CommunityExpTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("CommunityExpTextBox_LostFocus");
            try
            {
                if (string.IsNullOrEmpty(CommunityExpTextBox.Text))
                {
                    CommunityExpTextBox.Text = "1";
                }

                int Exp = Mathf.Clamp(int.Parse(CommunityExpTextBox.Text), 1, int.MaxValue);

                foreach (ListViewItem lvi in CommunityListView.SelectedItems)
                {
                    CommunityData cd = gameData.Community[lvi.Text];

                    cd.Favorability.Exp = Exp;
                    CommunityExpTextBox.Text = cd.Favorability.Exp.ToString();
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                CommunityExpTextBox.Text = CommunityExpTextBox.Tag.ToString();
            }
        }

        private void CommunityIsOpenCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            LogHelper.Debug("CommunityIsOpenCheckBox_CheckedChanged");
            try
            {
                foreach (ListViewItem lvi in CommunityListView.SelectedItems)
                {
                    CommunityData cd = gameData.Community[lvi.Text];

                    cd.isOpen = CommunityIsOpenCheckBox.Checked;

                    if (CommunityIsOpenCheckBox.Checked)
                    {
                        Game.GameData.NurturanceOrder.OpenCommunityOrder(lvi.Text);
                    }
                    else
                    {
                        Game.GameData.NurturanceOrder.CloseCommunityOrder(lvi.Text);
                    }
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void AddPartyButton_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("AddPartyButton_Click");
            try
            {
                foreach (ListViewItem lvi in CommunityListView.SelectedItems)
                {
                    Game.GameData.Party.AddParty(lvi.Text, false);
                }
                readParty();
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void RemovePartyButton_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("RemovePartyButton_Click");
            try
            {
                int index = -1;
                foreach (ListViewItem lvi in PartyListView.SelectedItems)
                {
                    index = lvi.Index;
                    Game.GameData.Party.RemoveParty(lvi.Text);
                }
                readParty();

                if (index == PartyListView.Items.Count)
                {
                    index--;
                }
                if (index != -1)
                {
                    PartyListView.Items[index].Selected = true;
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void PartyListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            LogHelper.Debug("PartyListView_SelectedIndexChanged");
            try
            {
                foreach (ListViewItem lvi in PartyListView.SelectedItems)
                {
                    if (lvi.Text == "Player")
                    {
                        RemovePartyButton.Enabled = false;
                    }
                    else
                    {
                        RemovePartyButton.Enabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void FlagAdd1Button_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("FlagAdd1Button_Click");
            try
            {
                messageLabel.Text = "";
                if (!saveFileIsSelected)
                {
                    messageLabel.Text = "请先选择一个存档";
                    LogHelper.Debug("请先选择一个存档");
                    return;
                }
                foreach (ListViewItem lvi in FlagListView.SelectedItems)  //选中项遍历  
                {
                    Game.GameData.Flag[lvi.Text] += 1;
                    lvi.SubItems[1].Text = Game.GameData.Flag[lvi.Text].ToString();
                }
                FlagListView.EndUpdate();
                readFlagLove();
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void FlagAdd10Button_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("FlagAdd10Button_Click");
            try
            {
                messageLabel.Text = "";
                if (!saveFileIsSelected)
                {
                    messageLabel.Text = "请先选择一个存档";
                    LogHelper.Debug("请先选择一个存档");
                    return;
                }
                foreach (ListViewItem lvi in FlagListView.SelectedItems)  //选中项遍历  
                {
                    Game.GameData.Flag[lvi.Text] += 10;
                    lvi.SubItems[1].Text = Game.GameData.Flag[lvi.Text].ToString();
                }
                FlagListView.EndUpdate();
                readFlagLove();
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void FlagSub1Button_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("FlagSub1Button_Click");
            try
            {
                messageLabel.Text = "";
                if (!saveFileIsSelected)
                {
                    messageLabel.Text = "请先选择一个存档";
                    LogHelper.Debug("请先选择一个存档");
                    return;
                }
                foreach (ListViewItem lvi in FlagListView.SelectedItems)  //选中项遍历  
                {
                    Game.GameData.Flag[lvi.Text] -= 1;
                    lvi.SubItems[1].Text = Game.GameData.Flag[lvi.Text].ToString();
                }
                FlagListView.EndUpdate();
                readFlagLove();
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void FlagSub10Button_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("FlagSub10Button_Click");
            try
            {
                messageLabel.Text = "";
                if (!saveFileIsSelected)
                {
                    messageLabel.Text = "请先选择一个存档";
                    LogHelper.Debug("请先选择一个存档");
                    return;
                }
                foreach (ListViewItem lvi in FlagListView.SelectedItems)  //选中项遍历  
                {
                    Game.GameData.Flag[lvi.Text] -= 10;
                    lvi.SubItems[1].Text = Game.GameData.Flag[lvi.Text].ToString();
                }
                FlagListView.EndUpdate();
                readFlagLove();
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void ctb_MasterLoveTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("ctb_MasterLoveTextBox_GotFocus");
            ctb_MasterLoveTextBox.Tag = ctb_MasterLoveTextBox.Text;
        }

        private void ctb_MasterLoveTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("ctb_MasterLoveTextBox_LostFocus");
            try
            {
                if (!saveFileIsSelected)
                {
                    messageLabel.Text = "请先选择一个存档";
                    LogHelper.Debug("请先选择一个存档");
                    return;
                }
                if (string.IsNullOrEmpty(ctb_MasterLoveTextBox.Text))
                {
                    ctb_MasterLoveTextBox.Text = "0";
                }

                int love = Mathf.Clamp(int.Parse(ctb_MasterLoveTextBox.Text), 0, int.MaxValue);

                Game.GameData.Flag["fg0201_MasterLove"] = love;

                ctb_MasterLoveTextBox.Text = Game.GameData.Flag["fg0201_MasterLove"].ToString();

                readFlag();
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                ctb_MasterLoveTextBox.Text = ctb_MasterLoveTextBox.Tag.ToString();
            }
        }

        private void dxl_MasterLoveTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("dxl_MasterLoveTextBox_GotFocus");

            dxl_MasterLoveTextBox.Tag = dxl_MasterLoveTextBox.Text;
        }

        private void dxl_MasterLoveTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("dxl_MasterLoveTextBox_LostFocus");
            try
            {
                if (!saveFileIsSelected)
                {
                    messageLabel.Text = "请先选择一个存档";
                    LogHelper.Debug("请先选择一个存档");
                    return;
                }
                if (string.IsNullOrEmpty(dxl_MasterLoveTextBox.Text))
                {
                    dxl_MasterLoveTextBox.Text = "0";
                }

                int love = Mathf.Clamp(int.Parse(dxl_MasterLoveTextBox.Text), 0, int.MaxValue);

                Game.GameData.Flag["fg0202_MasterLove"] = love;

                dxl_MasterLoveTextBox.Text = Game.GameData.Flag["fg0202_MasterLove"].ToString();

                readFlag();
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                dxl_MasterLoveTextBox.Text = dxl_MasterLoveTextBox.Tag.ToString();
            }

        }

        private void dh_MasterLoveTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("dh_MasterLoveTextBox_GotFocus");

            dh_MasterLoveTextBox.Tag = dh_MasterLoveTextBox.Text;
        }

        private void dh_MasterLoveTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("dh_MasterLoveTextBox_LostFocus");
            try
            {
                if (!saveFileIsSelected)
                {
                    messageLabel.Text = "请先选择一个存档";
                    LogHelper.Debug("请先选择一个存档");
                    return;
                }
                if (string.IsNullOrEmpty(dh_MasterLoveTextBox.Text))
                {
                    dh_MasterLoveTextBox.Text = "0";
                }

                int love = Mathf.Clamp(int.Parse(dh_MasterLoveTextBox.Text), 0, int.MaxValue);

                Game.GameData.Flag["fg0203_MasterLove"] = love;

                dh_MasterLoveTextBox.Text = Game.GameData.Flag["fg0203_MasterLove"].ToString();

                readFlag();
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                dh_MasterLoveTextBox.Text = dh_MasterLoveTextBox.Tag.ToString();
            }

        }

        private void lxp_MasterLoveTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("lxp_MasterLoveTextBox_GotFocus");

            lxp_MasterLoveTextBox.Tag = lxp_MasterLoveTextBox.Text;
        }

        private void lxp_MasterLoveTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("lxp_MasterLoveTextBox_LostFocus");
            try
            {
                if (!saveFileIsSelected)
                {
                    messageLabel.Text = "请先选择一个存档";
                    LogHelper.Debug("请先选择一个存档");
                    return;
                }
                if (string.IsNullOrEmpty(lxp_MasterLoveTextBox.Text))
                {
                    lxp_MasterLoveTextBox.Text = "0";
                }

                int love = Mathf.Clamp(int.Parse(lxp_MasterLoveTextBox.Text), 0, int.MaxValue);

                Game.GameData.Flag["fg0204_MasterLove"] = love;

                lxp_MasterLoveTextBox.Text = Game.GameData.Flag["fg0204_MasterLove"].ToString();

                readFlag();
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                lxp_MasterLoveTextBox.Text = lxp_MasterLoveTextBox.Tag.ToString();
            }

        }

        private void ht_MasterLoveTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("ht_MasterLoveTextBox_GotFocus");

            ht_MasterLoveTextBox.Tag = ht_MasterLoveTextBox.Text;
        }

        private void ht_MasterLoveTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("ht_MasterLoveTextBox_LostFocus");
            try
            {
                if (!saveFileIsSelected)
                {
                    messageLabel.Text = "请先选择一个存档";
                    LogHelper.Debug("请先选择一个存档");
                    return;
                }
                if (string.IsNullOrEmpty(ht_MasterLoveTextBox.Text))
                {
                    ht_MasterLoveTextBox.Text = "0";
                }

                int love = Mathf.Clamp(int.Parse(ht_MasterLoveTextBox.Text), 0, int.MaxValue);

                Game.GameData.Flag["fg0205_MasterLove"] = love;

                ht_MasterLoveTextBox.Text = Game.GameData.Flag["fg0205_MasterLove"].ToString();

                readFlag();
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                ht_MasterLoveTextBox.Text = ht_MasterLoveTextBox.Tag.ToString();
            }

        }

        private void tsz_MasterLoveTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("tsz_MasterLoveTextBox_GotFocus");

            tsz_MasterLoveTextBox.Tag = tsz_MasterLoveTextBox.Text;
        }

        private void tsz_MasterLoveTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("tsz_MasterLoveTextBox_LostFocus");
            try
            {
                if (!saveFileIsSelected)
                {
                    messageLabel.Text = "请先选择一个存档";
                    LogHelper.Debug("请先选择一个存档");
                    return;
                }
                if (string.IsNullOrEmpty(tsz_MasterLoveTextBox.Text))
                {
                    tsz_MasterLoveTextBox.Text = "0";
                }

                int love = Mathf.Clamp(int.Parse(tsz_MasterLoveTextBox.Text), 0, int.MaxValue);

                Game.GameData.Flag["fg0206_MasterLove"] = love;

                tsz_MasterLoveTextBox.Text = Game.GameData.Flag["fg0206_MasterLove"].ToString();

                readFlag();
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                tsz_MasterLoveTextBox.Text = tsz_MasterLoveTextBox.Tag.ToString();
            }

        }

        private void fxlh_MasterLoveTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("fxlh_MasterLoveTextBox_GotFocus");

            fxlh_MasterLoveTextBox.Tag = fxlh_MasterLoveTextBox.Text;
        }

        private void fxlh_MasterLoveTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("fxlh_MasterLoveTextBox_LostFocus");
            try
            {
                if (!saveFileIsSelected)
                {
                    messageLabel.Text = "请先选择一个存档";
                    LogHelper.Debug("请先选择一个存档");
                    return;
                }
                if (string.IsNullOrEmpty(fxlh_MasterLoveTextBox.Text))
                {
                    fxlh_MasterLoveTextBox.Text = "0";
                }

                int love = Mathf.Clamp(int.Parse(fxlh_MasterLoveTextBox.Text), 0, int.MaxValue);

                Game.GameData.Flag["fg0207_MasterLove"] = love;

                fxlh_MasterLoveTextBox.Text = Game.GameData.Flag["fg0207_MasterLove"].ToString();

                readFlag();
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                fxlh_MasterLoveTextBox.Text = fxlh_MasterLoveTextBox.Tag.ToString();
            }

        }

        private void ncc_MasterLoveTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("ncc_MasterLoveTextBox_GotFocus");

            ncc_MasterLoveTextBox.Tag = ncc_MasterLoveTextBox.Text;
        }

        private void ncc_MasterLoveTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("ncc_MasterLoveTextBox_LostFocus");
            try
            {
                if (!saveFileIsSelected)
                {
                    messageLabel.Text = "请先选择一个存档";
                    LogHelper.Debug("请先选择一个存档");
                    return;
                }
                if (string.IsNullOrEmpty(ncc_MasterLoveTextBox.Text))
                {
                    ncc_MasterLoveTextBox.Text = "0";
                }

                int love = Mathf.Clamp(int.Parse(ncc_MasterLoveTextBox.Text), 0, int.MaxValue);

                Game.GameData.Flag["fg0208_MasterLove"] = love;

                ncc_MasterLoveTextBox.Text = Game.GameData.Flag["fg0208_MasterLove"].ToString();

                readFlag();
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                ncc_MasterLoveTextBox.Text = ncc_MasterLoveTextBox.Tag.ToString();
            }

        }

        private void mrx_MasterLoveTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("mrx_MasterLoveTextBox_GotFocus");

            mrx_MasterLoveTextBox.Tag = mrx_MasterLoveTextBox.Text;
        }

        private void mrx_MasterLoveTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("mrx_MasterLoveTextBox_LostFocus");
            try
            {
                if (!saveFileIsSelected)
                {
                    messageLabel.Text = "请先选择一个存档";
                    LogHelper.Debug("请先选择一个存档");
                    return;
                }
                if (string.IsNullOrEmpty(mrx_MasterLoveTextBox.Text))
                {
                    mrx_MasterLoveTextBox.Text = "0";
                }

                int love = Mathf.Clamp(int.Parse(mrx_MasterLoveTextBox.Text), 0, int.MaxValue);

                Game.GameData.Flag["fg0209_MasterLove"] = love;

                mrx_MasterLoveTextBox.Text = Game.GameData.Flag["fg0209_MasterLove"].ToString();

                readFlag();
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                mrx_MasterLoveTextBox.Text = mrx_MasterLoveTextBox.Tag.ToString();
            }

        }

        private void j_MasterLoveTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("j_MasterLoveTextBox_GotFocus");

            j_MasterLoveTextBox.Tag = j_MasterLoveTextBox.Text;
        }

        private void j_MasterLoveTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("j_MasterLoveTextBox_LostFocus");

            try
            {
                if (!saveFileIsSelected)
                {
                    messageLabel.Text = "请先选择一个存档";
                    LogHelper.Debug("请先选择一个存档");
                    return;
                }
                if (string.IsNullOrEmpty(j_MasterLoveTextBox.Text))
                {
                    j_MasterLoveTextBox.Text = "0";
                }

                int love = Mathf.Clamp(int.Parse(j_MasterLoveTextBox.Text), 0, int.MaxValue);

                Game.GameData.Flag["fg0210_MasterLove"] = love;

                j_MasterLoveTextBox.Text = Game.GameData.Flag["fg0210_MasterLove"].ToString();

                readFlag();
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                j_MasterLoveTextBox.Text = j_MasterLoveTextBox.Tag.ToString();
            }
        }

        private void xx_NpcLoveTextBox_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("xx_NpcLoveTextBox_GotFocus");
            xx_NpcLoveTextBox.Tag = xx_NpcLoveTextBox.Text;

        }

        private void xx_NpcLoveTextBox_LostFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("xx_NpcLoveTextBox_LostFocus");
            try
            {
                if (!saveFileIsSelected)
                {
                    messageLabel.Text = "请先选择一个存档";
                    LogHelper.Debug("请先选择一个存档");
                    return;
                }
                if (string.IsNullOrEmpty(xx_NpcLoveTextBox.Text))
                {
                    xx_NpcLoveTextBox.Text = "0";
                }

                int love = Mathf.Clamp(int.Parse(xx_NpcLoveTextBox.Text), 0, int.MaxValue);

                Game.GameData.Flag["fg0301_NpcLove"] = love;

                xx_NpcLoveTextBox.Text = Game.GameData.Flag["fg0301_NpcLove"].ToString();

                readFlag();
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);

                xx_NpcLoveTextBox.Text = xx_NpcLoveTextBox.Tag.ToString();
            }

        }

        private void QuestListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            LogHelper.Debug("QuestListView_SelectedIndexChanged");
            try
            {
                foreach (ListViewItem lvi in QuestListView.SelectedItems)
                {
                    if (!saveFileIsSelected)
                    {
                        messageLabel.Text = "请先选择一个存档";
                        LogHelper.Debug("请先选择一个存档");
                        QuestListView.SelectedItems.Clear();
                        return;
                    }
                    if (Game.GameData.Quest.IsInProgress(lvi.Text))
                    {
                        QuestStateComboBox.SelectedIndex = 1;
                    }
                    else if (Game.GameData.Quest.IsPassed(lvi.Text))
                    {
                        QuestStateComboBox.SelectedIndex = 2;
                    }
                    else
                    {
                        QuestStateComboBox.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void ShowAllQuestComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            LogHelper.Debug("ShowAllQuestComboBox_SelectedIndexChanged");
            try
            {
                if (!isSaveFileSelecting)
                {
                    readQuest();
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void QuestChangeState1Button_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("QuestChangeState1Button_Click");
            try
            {
                foreach (ListViewItem lvi in QuestListView.SelectedItems)
                {
                    if (Game.GameData.Quest.TrackedKind != QuestManager.QuestKind.None && Game.GameData.Quest.InProgress[(int)Game.GameData.Quest.TrackedKind] == lvi.Text)
                    {
                        Game.GameData.Quest.InProgress[(int)Game.GameData.Quest.TrackedKind] = "";
                    }
                    Game.GameData.Quest.Passed.Remove(lvi.Text);
                    QuestStateComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void QuestChangeState2Button_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("QuestChangeState2Button_Click");
            try
            {
                foreach (ListViewItem lvi in QuestListView.SelectedItems)
                {
                    if (Game.GameData.Quest.TrackedKind != QuestManager.QuestKind.None)
                    {
                        Game.GameData.Quest.InProgress[(int)Game.GameData.Quest.TrackedKind] = lvi.Text;
                    }
                    Game.GameData.Quest.Passed.Remove(lvi.Text);
                    QuestStateComboBox.SelectedIndex = 1;
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void QuestChangeState3Button_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("QuestChangeState3Button_Click");
            try
            {
                foreach (ListViewItem lvi in QuestListView.SelectedItems)
                {
                    if (Game.GameData.Quest.TrackedKind != QuestManager.QuestKind.None && Game.GameData.Quest.InProgress[(int)Game.GameData.Quest.TrackedKind] == lvi.Text)
                    {
                        Game.GameData.Quest.InProgress[(int)Game.GameData.Quest.TrackedKind] = "";
                    }
                    if (!Game.GameData.Quest.Passed.Contains(lvi.Text))
                    {
                        Game.GameData.Quest.Passed.Add(lvi.Text);
                    }
                    QuestStateComboBox.SelectedIndex = 2;
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void SetCurrentElectiveButton_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("SetCurrentElectiveButton_Click");
            try
            {
                foreach (ListViewItem lvi in ElectiveListView.SelectedItems)
                {
                    int index = lvi.Index;
                    Game.GameData.Elective.Id = lvi.Text;
                    if (!Data.Get<Elective>(lvi.Text).IsRepeat)
                    {
                        if (!Game.GameData.Elective.Triggered.Contains(lvi.Text))
                        {
                            Game.GameData.Elective.Triggered.Add(lvi.Text);
                        }
                    }
                    readElective();
                    ElectiveListView.Items[index].Selected = true;
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }

        }

        private void NurturanceOrderListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            LogHelper.Debug("NurturanceOrderListView_SelectedIndexChanged");
            try
            {
                foreach (ListViewItem lvi in NurturanceOrderListView.SelectedItems)
                {
                    NurturanceOrderStateTextBox.Text = getNurturanceOrderContain(Game.GameData.NurturanceOrder.Root, lvi.Text) ? "开启" : "关闭";
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void NurturanceOrderOpenButton_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("NurturanceOrderOpenButton_Click");
            try
            {
                foreach (ListViewItem lvi in NurturanceOrderListView.SelectedItems)
                {
                    Game.GameData.NurturanceOrder.OpenOrder(lvi.Text);
                    NurturanceOrderStateTextBox.Text = getNurturanceOrderContain(Game.GameData.NurturanceOrder.Root, lvi.Text) ? "开启" : "关闭";

                    lvi.SubItems[2].Text = getNurturanceOrderContain(Game.GameData.NurturanceOrder.Root, lvi.Text) ? "开启" : "关闭";
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }

        }
        private void NurturanceOrderCloseButton_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("NurturanceOrderCloseButton_Click");
            try
            {
                foreach (ListViewItem lvi in NurturanceOrderListView.SelectedItems)
                {
                    Game.GameData.NurturanceOrder.CloseOrder(lvi.Text);
                    NurturanceOrderStateTextBox.Text = getNurturanceOrderContain(Game.GameData.NurturanceOrder.Root, lvi.Text) ? "开启" : "关闭";
                    lvi.SubItems[2].Text = getNurturanceOrderContain(Game.GameData.NurturanceOrder.Root, lvi.Text) ? "开启" : "关闭";
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        public bool getNurturanceOrderContain(Tree<Nurturance> root, string nurturanceId)
        {
            try
            {
                if (root.Value != null && root.Value.Id == nurturanceId)
                {
                    return true;
                }

                for (int i = 0; i < root.Children.Count; i++)
                {
                    bool contains = getNurturanceOrderContain(root.Children[i], nurturanceId);
                    if (contains)
                    {
                        return contains;
                    }
                    else
                    {
                        continue;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
                return false;
            }
        }

        private void BookListView_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("BookListView_GotFocus");
            AddBookButton.Enabled = true;
            RemoveBookButton.Enabled = false;
        }

        private void HavingBookListView_GotFocus(object sender, EventArgs e)
        {
            LogHelper.Debug("HavingBookListView_GotFocus");
            AddBookButton.Enabled = false;
            RemoveBookButton.Enabled = true;
        }

        private void AddBookButton_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("AddBookButton_Click");
            try
            {
                if (!saveFileIsSelected)
                {
                    messageLabel.Text = "请先选择一个存档";
                    LogHelper.Debug("请先选择一个存档");
                    return;
                }
                foreach (ListViewItem lvi in BookListView.SelectedItems)
                {
                    Game.GameData.ReadBookManager.GetBook(lvi.Text);
                    readBook();
                    int index = HavingBookListView.FindItemWithText(lvi.Text).Index;
                    HavingBookListView.EnsureVisible(index);
                    HavingBookListView.Items[index].Selected = true;
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void RemoveBookButton_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("RemoveBookButton_Click");
            try
            {
                foreach (ListViewItem lvi in HavingBookListView.SelectedItems)
                {
                    int index = lvi.Index;
                    Game.GameData.ReadBookManager.Remove(lvi.Text);
                    HavingBookListView.Items.Remove(lvi);
                    if (index == HavingBookListView.Items.Count)
                    {
                        index = HavingBookListView.Items.Count - 1;
                    }
                    if (index != -1)
                    {
                        HavingBookListView.Items[index].Selected = true;
                    }
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void LearnAlchemyButton_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("LearnAlchemyButton_Click");
            try
            {
                foreach (ListViewItem lvi in AlchemyListView.SelectedItems)
                {
                    Game.GameData.Alchemy.Learn(lvi.Text);
                    lvi.SubItems[3].Text = Game.GameData.Alchemy.Learned.Contains(lvi.Text) ? "是" : "否";
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void AbolishAlchemyButton_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("AbolishAlchemyButton_Click");
            try
            {
                foreach (ListViewItem lvi in AlchemyListView.SelectedItems)
                {
                    Game.GameData.Alchemy.Learned.Remove(lvi.Text);
                    lvi.SubItems[3].Text = Game.GameData.Alchemy.Learned.Contains(lvi.Text) ? "是" : "否";
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void OpenForgeFightButton_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("OpenForgeFightButton_Click");
            try
            {
                foreach (ListViewItem lvi in ForgeFightListView.SelectedItems)
                {
                    Game.GameData.Forge.Open(lvi.Text);
                    lvi.SubItems[4].Text = Game.GameData.Forge.Opened.Contains(lvi.Text) ? "是" : "否";
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void CloseForgeFightButton_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("CloseForgeFightButton_Click");
            try
            {
                foreach (ListViewItem lvi in ForgeFightListView.SelectedItems)
                {
                    Game.GameData.Forge.Opened.Remove(lvi.Text);
                    lvi.SubItems[4].Text = Game.GameData.Forge.Opened.Contains(lvi.Text) ? "是" : "否";
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void OpenForgeBladeAndSwordButton_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("OpenForgeBladeAndSwordButton_Click");
            try
            {
                foreach (ListViewItem lvi in ForgeBladeAndSwordListView.SelectedItems)
                {
                    Game.GameData.Forge.Open(lvi.Text);
                    lvi.SubItems[4].Text = Game.GameData.Forge.Opened.Contains(lvi.Text) ? "是" : "否";
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void CloseForgeBladeAndSwordButton_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("CloseForgeBladeAndSwordButton_Click");
            try
            {
                foreach (ListViewItem lvi in ForgeBladeAndSwordListView.SelectedItems)
                {
                    Game.GameData.Forge.Opened.Remove(lvi.Text);
                    lvi.SubItems[4].Text = Game.GameData.Forge.Opened.Contains(lvi.Text) ? "是" : "否";
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void OpenForgeLongAndShortButton_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("OpenForgeLongAndShortButton_Click");
            try
            {
                foreach (ListViewItem lvi in ForgeLongAndShortListView.SelectedItems)
                {
                    Game.GameData.Forge.Open(lvi.Text);
                    lvi.SubItems[4].Text = Game.GameData.Forge.Opened.Contains(lvi.Text) ? "是" : "否";
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void CloseForgeLongAndShortButton_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("CloseForgeLongAndShortButton_Click");
            try
            {
                foreach (ListViewItem lvi in ForgeLongAndShortListView.SelectedItems)
                {
                    Game.GameData.Forge.Opened.Remove(lvi.Text);
                    lvi.SubItems[4].Text = Game.GameData.Forge.Opened.Contains(lvi.Text) ? "是" : "否";
                }

            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void OpenForgeQimenButton_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("OpenForgeQimenButton_Click");
            try
            {
                foreach (ListViewItem lvi in ForgeQimenListView.SelectedItems)
                {
                    Game.GameData.Forge.Open(lvi.Text);
                    lvi.SubItems[4].Text = Game.GameData.Forge.Opened.Contains(lvi.Text) ? "是" : "否";
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void CloseForgeQimenButton_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("CloseForgeQimenButton_Click");
            try
            {
                foreach (ListViewItem lvi in ForgeQimenListView.SelectedItems)
                {
                    Game.GameData.Forge.Opened.Remove(lvi.Text);
                    lvi.SubItems[4].Text = Game.GameData.Forge.Opened.Contains(lvi.Text) ? "是" : "否";
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void OpenForgeArmorButton_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("OpenForgeArmorButton_Click");
            try
            {
                foreach (ListViewItem lvi in ForgeArmorListView.SelectedItems)
                {
                    Game.GameData.Forge.Open(lvi.Text);
                    lvi.SubItems[4].Text = Game.GameData.Forge.Opened.Contains(lvi.Text) ? "是" : "否";
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void CloseForgeArmorButton_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("CloseForgeArmorButton_Click");
            try
            {
                foreach (ListViewItem lvi in ForgeArmorListView.SelectedItems)
                {
                    Game.GameData.Forge.Opened.Remove(lvi.Text);
                    lvi.SubItems[4].Text = Game.GameData.Forge.Opened.Contains(lvi.Text) ? "是" : "否";
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void AddShopButton_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("AddShopButton_Click");
            try
            {
                foreach (ListViewItem lvi in ShopListView.SelectedItems)
                {
                    ShopSoldOutInfo shopSoldOutInfo = Game.GameData.Shop.SoldOuts.Find((ShopSoldOutInfo x) => x.SoldOutId == lvi.Text);
                    Game.GameData.Shop.SoldOuts.Remove(shopSoldOutInfo);

                    Shop shop = Data.Get<Shop>(lvi.Text);
                    lvi.SubItems[6].Text = shopIsSoldOut(shop);
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void RemoveShopButton_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("RemoveShopButton_Click");
            try
            {
                foreach (ListViewItem lvi in ShopListView.SelectedItems)
                {
                    ShopSoldOutInfo item = new ShopSoldOutInfo
                    {
                        SoldOutId = lvi.Text,
                        SoldOutRound = Game.GameData.Round.CurrentRound
                    };
                    Game.GameData.Shop.SoldOuts.Add(item);

                    Shop shop = Data.Get<Shop>(lvi.Text);
                    lvi.SubItems[6].Text = shopIsSoldOut(shop);
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void SearchFlagButton_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("SearchFlagButton_Click");
            try
            {
                SearchFlagResultLabel.Text = "";
                string searchFlag = SearchFlagTextBox.Text;

                bool isSearched = false;
                if (FlagListView.Items.Count != 0)
                {
                    int startIndex = 0;

                    if (FlagListView.SelectedItems != null && FlagListView.SelectedItems.Count != 0)
                    {
                        startIndex = FlagListView.SelectedItems[0].Index + 1;
                    }

                    if (startIndex == FlagListView.Items.Count)
                    {
                        startIndex = 0;
                    }
                    int index = startIndex;

                    do
                    {
                        ListViewItem lvi = FlagListView.Items[index];

                        if (lvi.Text.Contains(searchFlag) || lvi.SubItems[2].Text.Contains(searchFlag))
                        {
                            lvi.Selected = true;
                            isSearched = true;
                            FlagListView.EnsureVisible(lvi.Index);
                            break;
                        }
                        index++;

                        if (index == FlagListView.Items.Count)
                        {
                            index = 0;
                        }
                    } while (index != startIndex);
                }
                if (!isSearched)
                {
                    SearchFlagResultLabel.Text = "未找到该旗标";
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void fixBug()
        {
            LogHelper.Debug("fixBug");
            try
            {
                Game.GameData.Character["Player"].Skill.Remove("");

                gameData.Inventory.Remove("");
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void PropsSearchButton_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("PropsSearchButton_Click");
            try
            {
                SearchPropsResultLabel.Text = "";
                string searchProps = SearchPropsTextBox.Text;

                bool isSearched = false;
                if (PropsListView.Items.Count != 0)
                {
                    int startIndex = 0;

                    if (PropsListView.SelectedItems != null && PropsListView.SelectedItems.Count != 0)
                    {
                        startIndex = PropsListView.SelectedItems[0].Index + 1;
                    }

                    if (startIndex == PropsListView.Items.Count)
                    {
                        startIndex = 0;
                    }
                    int index = startIndex;

                    do
                    {
                        ListViewItem lvi = PropsListView.Items[index];

                        if (lvi.Text.Contains(searchProps) || lvi.SubItems[1].Text.Contains(searchProps))
                        {
                            lvi.Selected = true;
                            isSearched = true;
                            PropsListView.EnsureVisible(lvi.Index);
                            break;
                        }
                        index++;

                        if (index == PropsListView.Items.Count)
                        {
                            index = 0;
                        }
                    } while (index != startIndex);
                }
                if (!isSearched)
                {
                    SearchPropsResultLabel.Text = "未找到该物品";
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void InventorySearchButton_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("InventorySearchButton_Click");
            try
            {
                SearchInventoryResultLabel.Text = "";
                string searchInventory = SearchInventoryTextBox.Text;

                bool isSearched = false;
                if (InventoryListView.Items.Count != 0)
                {
                    int startIndex = 0;

                    if (InventoryListView.SelectedItems != null && InventoryListView.SelectedItems.Count != 0)
                    {
                        startIndex = InventoryListView.SelectedItems[0].Index + 1;
                    }

                    if (startIndex == InventoryListView.Items.Count)
                    {
                        startIndex = 0;
                    }
                    int index = startIndex;

                    do
                    {
                        ListViewItem lvi = InventoryListView.Items[index];

                        if (lvi.Text.Contains(searchInventory) || lvi.SubItems[1].Text.Contains(searchInventory))
                        {
                            lvi.Selected = true;
                            isSearched = true;
                            InventoryListView.EnsureVisible(lvi.Index);
                            break;
                        }
                        index++;

                        if (index == InventoryListView.Items.Count)
                        {
                            index = 0;
                        }
                    } while (index != startIndex);
                }
                if (!isSearched)
                {
                    SearchInventoryResultLabel.Text = "未找到该物品";
                }
            }
            catch (Exception ex)
            {
                messageLabel.Text = ex.Message;
                LogHelper.Debug(ex.Message + "\n" + ex.InnerException);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            LogHelper.Debug("button1_Click");
            string flag = SearchFlagTextBox.Text;
            if (gameData.Flag.ContainsKey(flag))
            {
                messageLabel.Text = "该旗标已存在";
                LogHelper.Debug("该旗标已存在");
            }
            else
            {
                gameData.Flag[flag] = 0;
                readFlag();
            }
        }
    }
}
