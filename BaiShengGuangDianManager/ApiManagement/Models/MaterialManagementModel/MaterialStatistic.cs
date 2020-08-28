using System;

namespace ApiManagement.Models.MaterialManagementModel
{
    public enum MaterialStatisticInterval
    {
        不设置,
        天,
        周,
        月,
        年,
    }
    public class MaterialStatistic
    {
        public DateTime Time { get; set; }
        /// <summary>
        /// 货品编号Id
        /// </summary>
        public int BillId { get; set; }
        /// <summary>
        /// 货品编号
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// 类别Id
        /// </summary>
        public int CategoryId { get; set; }
        /// <summary>
        /// 类别
        /// </summary>
        public string Category { get; set; }
        /// <summary>
        /// 货品名称Id
        /// </summary>
        public int NameId { get; set; }
        /// <summary>
        /// 货品名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 供应商Id
        /// </summary>
        public int SupplierId { get; set; }
        /// <summary>
        /// 供应商
        /// </summary>
        public string Supplier { get; set; }
        /// <summary>
        /// 规格Id
        /// </summary>
        public int SpecificationId { get; set; }
        /// <summary>
        /// 规格
        /// </summary>
        public string Specification { get; set; }
        /// <summary>
        /// 位置Id
        /// </summary>
        public int SiteId { get; set; }
        /// <summary>
        /// 位置
        /// </summary>
        public string Site { get; set; }
        /// <summary>
        /// 单位
        /// </summary>
        public string Unit { get; set; }
        /// <summary>
        /// 最低库存
        /// </summary>
        public decimal Stock { get; set; }
        /// <summary>
        /// 上次数量
        /// </summary>
        public decimal LastNumber { get; set; }
        /// <summary>
        /// 上次单价
        /// </summary>
        public decimal LastPrice { get; set; }
        /// <summary>
        /// 上次总价
        /// </summary>
        public decimal LastAmount { get; set; }
        /// <summary>
        /// 今日数量
        /// </summary>
        public decimal TodayNumber { get; set; }
        /// <summary>
        /// 今日单价
        /// </summary>
        public decimal TodayPrice { get; set; }
        /// <summary>
        /// 今日价格
        /// </summary>
        public decimal TodayAmount { get; set; }
        /// <summary>
        /// 今日入库数量
        /// </summary>
        public decimal Increase { get; set; }
        /// <summary>
        /// 今日入库金额
        /// </summary>
        public decimal IncreaseAmount { get; set; }
        /// <summary>
        /// 今日领用数量
        /// </summary>
        public decimal Consume { get; set; }
        /// <summary>
        /// 今日领用金额
        /// </summary>
        public decimal ConsumeAmount { get; set; }
        /// <summary>
        /// 今日冲正增加数量
        /// </summary>
        public decimal CorrectIn { get; set; }
        /// <summary>
        /// 今日冲正增加金额
        /// </summary>
        public decimal CorrectInAmount { get; set; }
        /// <summary>
        /// 今日冲正减少数量
        /// </summary>
        public decimal CorrectCon { get; set; }
        /// <summary>
        /// 今日冲正减少金额
        /// </summary>
        public decimal CorrectConAmount { get; set; }
        /// <summary>
        /// 今日冲正数量
        /// </summary>
        public decimal Correct { get; set; }
        /// <summary>
        /// 今日冲正金额
        /// </summary>
        public decimal CorrectAmount { get; set; }

        public void Init()
        {
            LastNumber = 0;
            LastPrice = 0;
            LastAmount = 0;
            Increase = 0;
            IncreaseAmount = 0;
            Consume = 0;
            ConsumeAmount = 0;
            CorrectIn = 0;
            CorrectInAmount = 0;
            CorrectCon = 0;
            CorrectConAmount = 0;
            Correct = 0;
            CorrectAmount = 0;
        }

        public bool Valid()
        {
            return TodayNumber
                + LastNumber
                + Increase
                + Consume
                + Increase
                + Consume
                + CorrectIn
                + CorrectCon
                + Correct != 0;
        }
    }
}
