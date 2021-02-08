namespace ApiManagement.Models.SmartFactoryModel
{
    public enum SmartWorkOrderState
    {
        未加工 = 0,
        加工中 = 1,
        暂停中 = 2,
        已取消 = 3,
        已完成 = 4,
        等待中 = 5
    }

    public enum SmartTaskOrderState
    {
        未加工 = 0,
        加工中 = 1,
        暂停中 = 2,
        已取消 = 3,
        已完成 = 4,
        等待中 = 5
    }

    public enum SmartFlowCardState
    {
        未加工 = 0,
        加工中 = 1,
        暂停中 = 2,
        已取消 = 3,
        已完成 = 4,
        等待中 = 5
    }

    public enum SmartFlowCardProcessState
    {
        未加工 = 0,
        加工中 = 1,
        暂停中 = 2,
        已取消 = 3,
        已完成 = 4,
        等待中 = 5,
    }

    public enum SmartLineState
    {
        未加工 = 0,
        加工中 = 1,
        暂停中 = 2,
        已取消 = 3,
        已完成 = 4,
        等待中 = 5
    }

    public enum ProcessFault
    {
        正常 = 0,
        #region 1 - 999 设备
        设备繁忙 = 1,
        设备故障,
        设备数据异常,
        #endregion

        #region 1000 - 2999 生产
        产量未达标 = 1000,
        合格率低,
        缺少工艺,
        缺少工人,
        缺少原材料,
        #endregion

        #region 3000 - 3999 其他
        生产流程缺失 = 3000,
        #endregion
    }

    public enum ProcessFaultDeal
    {
        处理 = 0,
        #region 1 - 999 设备
        设备更换 = 1,
        设备报修,
        #endregion

        #region 1000 - 2999 生产

        #endregion

        #region 3000 - 3999 其他
        #endregion
    }

    public enum SmartDeviceOperateState
    {
        未加工 = 0,
        加工中 = 1,
        暂停中 = 2,
        故障中 = 3,
        准备中 = 4,
        缺失 = 5,
    }

    public enum ScheduleState
    {
        成功 = 0,
        工人繁忙 = 1,
        缺少工人 = 2,
        设备繁忙 = 3,
        缺少设备 = 4,
    }
    /// <summary>
    /// 准时率
    /// </summary>
    public enum RiskLevelState
    {
        低 = 0,
        中 = 1,
        高 = 2
    }
    /// <summary>
    /// 操作工状态
    /// </summary>
    public enum SmartOperatorState
    {
        全部 = 0,
        正常 = 1,
        休息 = 2,
    }

    public enum SmartDeviceState
    {
        全部 = 0,
        正常 = 1,
        故障 = 2,
        报废 = 3,
    }
    public enum SmartProductCapacityError
    {
        正常 = 0,
        产能未设置 = 1,
        合格率未设置 = 2,
    }
    public enum SmartKanBanUnit
    {
        秒 = 0,
        分 = 1,
        小时 = 2,
    }
    public enum SmartKanBanError
    {
        正常 = 0,
        合格率低 = 1,
        影响交货 = 2,
    }
}