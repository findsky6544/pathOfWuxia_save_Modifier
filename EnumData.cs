using Heluo;
using System;
using System.Reflection;

namespace 侠之道存档修改器
{
    public static class EnumData
    {
        public static string GetDisplayName(this Enum eum)
        {
            var type = eum.GetType();//先获取这个枚举的类型
            var field = type.GetField(eum.ToString());//通过这个类型获取到值
            var obj = (DisplayNameAttribute)field.GetCustomAttribute(typeof(DisplayNameAttribute));//得到特性
            return obj.Name ?? "";
        }
        public static string GetDisplayName(bool value)
        {
            if (value)
            {
                return "是";
            }
            return "否";
        }

        public enum PropsType
        {
            武器 = 1,
            防具,
            饰品,
            书籍,
            药品,
            任务物品,
            礼物,
            图纸药方
        }

        public enum Year
        {
            一 = 1,
            二,
            三
        }

        public enum Month
        {
            一 = 1,
            二,
            三,
            四,
            五,
            六,
            七,
            八,
            九,
            十,
            十一,
            十二
        }

        public enum RoundOfMonth
        {
            月初 = 1,
            上旬,
            中旬,
            下旬,
            月底
        }

        public enum Time
        {
            白天 = 1,
            夜晚
        }

        public enum GameLevel
        {
            逍遥 = 1,
            磨炼,
            凶险,
            绝境
        }

        public enum QuestState
        {
            未领取,
            进行中,
            已完成
        }

        public enum QuestSchedule
        {
            半日,
            一日,
            不耗
        }

        public enum showAllQuest
        {
            仅显示传书,
            显示所有任务
        }

        public enum ElectiveState
        {
            未进修,
            已进修
        }
    }
}
