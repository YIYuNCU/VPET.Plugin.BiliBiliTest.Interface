namespace VPET.Evian.BiliBiliTest.Core
{
    /// <summary>
    /// 金额单位。B站推送的礼物金额、观众的金钱在内部都用金瓜子（1000=1元）保存；
    /// 设置页里填的价格、对观众播报的金额都用电池（1电池=100金瓜子=0.1元）
    /// </summary>
    public static class Currency
    {
        public const int GoldPerBattery = 100;
        public const int GoldPerYuan = 1000;
    }
}
