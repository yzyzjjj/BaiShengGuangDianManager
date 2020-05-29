using System.Collections.Generic;
using System.ComponentModel;

namespace ApiManagement.Models.ManufactureModel
{
    /// <summary>
    /// 生产计划状态
    /// 0 待下发 1 已下发 2 进行中  3 已完成
    /// </summary>
    public enum ManufacturePlanState
    {
        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        Default = 0,
        /// <summary>
        /// 待下发
        /// </summary>
        [Description("待下发")]
        Wait = 1,
        /// <summary>
        /// 已下发
        /// </summary>
        [Description("已下发")]
        Assigned = 2,
        /// <summary>
        /// 进行中
        /// </summary>
        [Description("进行中")]
        Doing = 3,
        /// <summary>
        /// 已完成
        /// </summary>
        [Description("已完成")]
        Done = 4,
    }

    /// <summary>
    /// 生产计划状态
    /// 0 待下发 1 已下发 2 进行中  3 已完成
    /// </summary>
    public enum ManufacturePlanItemState
    {
        /// <summary>
        /// 待下发
        /// </summary>
        [Description("待下发")]
        WaitAssign = -1,
        /// <summary>
        /// 
        /// </summary>
        [Description("已下发")]
        Default = 0,
    }

    /// <summary>
    /// 任务状态
    /// 0 等待中 1 进行中 2 已暂停 3 已完成 4 停止中 5 待返工 6 返工中 7 待检验 8 检验中
    /// </summary>
    public enum ManufacturePlanTaskState
    {
        /// <summary>
        /// 待下发
        /// </summary>
        [Description("待下发")]
        WaitAssign = -1,
        /// <summary>
        /// 等待中
        /// </summary>
        [Description("等待中")]
        Wait = 0,
        /// <summary>
        /// 进行中
        /// </summary>
        [Description("进行中")]
        Doing = 1,
        /// <summary>
        /// 已暂停
        /// </summary>
        [Description("已暂停")]
        Pause = 2,
        /// <summary>
        /// 已完成
        /// </summary>
        [Description("已完成")]
        Done = 3,
        /// <summary>
        /// 停止中
        /// </summary>
        [Description("停止中")]
        Stop = 4,
        /// <summary>
        /// 待返工
        /// </summary>
        [Description("待返工")]
        WaitRedo = 5,
        /// <summary>
        /// 返工中
        /// </summary>
        [Description("返工中")]
        Redo = 6,
        /// <summary>
        /// 待检验
        /// </summary>
        [Description("待检验")]
        WaitCheck = 7,
        /// <summary>
        /// 检验中
        /// </summary>
        [Description("检验中")]
        Checking = 8,
    }

    /// <summary>
    /// 检验任务结果
    /// 0 未检验  1 合格 2 返工 3 阻塞
    /// </summary>
    public enum ManufacturePlanCheckState
    {
        /// <summary>
        /// 未检验
        /// </summary>
        [Description("未检验")]
        Wait = 0,
        /// <summary>
        /// 合格
        /// </summary>
        [Description("合格")]
        Pass = 1,
        /// <summary>
        /// 返工
        /// </summary>
        [Description("返工")]
        Redo = 2,
        /// <summary>
        /// 阻塞
        /// </summary>
        [Description("阻塞")]
        Block = 3,
    }

    /// <summary>
    /// 检验任务结果
    /// 0 未检验  1 通过 2 不通过
    /// </summary>
    public enum ManufacturePlanCheckItemState
    {
        /// <summary>
        /// 未检验
        /// </summary>
        [Description("未检验")]
        Wait = 0,
        /// <summary>
        /// 合格
        /// </summary>
        [Description("通过")]
        Pass = 1,
        /// <summary>
        /// 返工
        /// </summary>
        [Description("返工")]
        Redo = 2,
        /// <summary>
        /// 阻塞
        /// </summary>
        [Description("阻塞")]
        Block = 3,
    }

    /// <summary>
    /// 日志
    /// </summary>
    public enum ManufactureLogType
    {
        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        Default = 0,
        /// <summary>
        /// 创建计划
        /// </summary>
        [Description("创建计划")]
        PlanCreate = 1,
        /// <summary>
        /// 下发计划
        /// </summary>
        [Description("下发计划")]
        PlanAssigned,
        /// <summary>
        /// 编辑计划
        /// </summary>
        [Description("编辑计划")]
        PlanUpdate,
        /// <summary>
        /// 删除计划
        /// </summary>
        [Description("删除计划")]
        PlanDelete,
        /// <summary>
        /// 编辑计划任务
        /// </summary>
        [Description("编辑计划任务")]
        PlanUpdateItem,


        /// <summary>
        /// 创建任务
        /// </summary>
        [Description("创建任务")]
        TaskCreate = 50,
        /// <summary>
        /// 编辑任务
        /// </summary>
        [Description("编辑任务")]
        TaskUpdate,
        /// <summary>
        /// 删除任务
        /// </summary>
        [Description("删除任务")]
        TaskDelete,
        /// <summary>
        /// 下发计划任务
        /// </summary>
        [Description("下发计划任务")]
        TaskAssigned,
        /// <summary>
        /// 开始任务
        /// </summary>
        [Description("开始任务")]
        StartTask,
        /// <summary>
        /// 暂停任务
        /// </summary>
        [Description("暂停任务")]
        PauseTask,
        /// <summary>
        /// 完成任务
        /// </summary>
        [Description("完成任务")]
        FinishTask,

        /// <summary>
        /// 生成检验项
        /// </summary>
        [Description("生成检验项")]
        CheckAssigned,
        /// <summary>
        /// 更新检验项
        /// </summary>
        [Description("更新检验项")]
        UpdateCheckItem,
        /// <summary>
        /// 更新检验结果
        /// </summary>
        [Description("更新检验结果")]
        UpdateCheckResult,

        /// <summary>
        /// 启动任务
        /// </summary>
        [Description("启动任务")]
        StartUpTask,
        /// <summary>
        /// 停止任务
        /// </summary>
        [Description("停止任务")]
        StopTask,

        /// <summary>
        /// 修改默认模板  修改了{0}，{1}改为{2}
        /// </summary>
        UpdateFormat = 100,
        /// <summary>
        /// 修改关联  为0   修改了{0}，关联到序号{2}
        /// </summary>
        UpdateRelationFormat1,
        /// <summary>
        /// 修改关联  不为0  修改了{0}，序号{1}改为序号{2}
        /// </summary>
        UpdateRelationFormat2,
        /// <summary>
        /// 修改图片   修改了图片
        /// </summary>
        UpdateImagesFormat,
        /// <summary>
        /// 删除计划任务   删除了 任务序号1
        /// </summary>
        DeletePlanTaskFormat,
        /// <summary>
        /// 修改计划任务   修改了 任务序号2
        /// </summary>
        UpdatePlanTaskFormat,
        /// <summary>
        /// 修改计划任务详情   将{0}由{1}改为{2}
        /// </summary>
        UpdatePlanTaskItemFormat,
        /// <summary>
        /// 新增计划任务   新增了 任务序号3
        /// </summary>
        AddPlanTaskFormat,

        /// <summary>
        /// 任务用时   已用时：天小时分
        /// </summary>
        TaskConsume,
    }
    public class ManufactureLogConfig
    {
        public static Dictionary<ManufactureLogType, string> LogFormat = new Dictionary<ManufactureLogType, string>
        {
            {ManufactureLogType.PlanCreate, "{0}，由{1}创建计划。"},
            {ManufactureLogType.PlanAssigned, "{0}，由{1}下发计划。"},
            {ManufactureLogType.PlanUpdate,  "{0}，由{1}编辑计划。"},
            {ManufactureLogType.PlanDelete,  "{0}，由{1}删除计划。"},
            {ManufactureLogType.PlanUpdateItem,  "{0}，由{1}编辑计划任务。"},

            {ManufactureLogType.TaskCreate,"{0}，由{1}创建任务。"},
            {ManufactureLogType.TaskAssigned, "{0}，由{1}下发任务。"},
            {ManufactureLogType.TaskUpdate, "{0}，由{1}编辑任务。"},
            {ManufactureLogType.TaskDelete,  "{0}，由{1}删除任务。"},
            {ManufactureLogType.StartTask,  "{0}，由{1}开始任务。"},
            {ManufactureLogType.PauseTask,  "{0}，由{1}暂停任务。"},
            {ManufactureLogType.FinishTask,  "{0}，由{1}完成任务。"},
            {ManufactureLogType.CheckAssigned,  "{0}，由{1}生成检验项。"},
            {ManufactureLogType.UpdateCheckItem,  "{0}，由{1}更新检验项。"},
            {ManufactureLogType.UpdateCheckResult,  "{0}，由{1}更新检验结果。"},

            {ManufactureLogType.UpdateFormat, "修改了{0}，由{1}改为{2}。"},
            {ManufactureLogType.UpdateRelationFormat1, "修改了{0}，关联到序号{2}。"},
            {ManufactureLogType.UpdateRelationFormat2, "修改了{0}，序号{1}改为序号{2}。"},
            {ManufactureLogType.UpdateImagesFormat, "修改了图片。"},

            {ManufactureLogType.DeletePlanTaskFormat, "删除了任务序号{0}。"},
            {ManufactureLogType.UpdatePlanTaskFormat,"修改了任务序号{0}，"},
            {ManufactureLogType.UpdatePlanTaskItemFormat, "将{0}由{1}改为{2}"},
            {ManufactureLogType.AddPlanTaskFormat, "新增了任务序号{0}。"},

            {ManufactureLogType.TaskConsume, "已用时：{0}。"},
        };
    }

    public class ManufactureDescription : DescriptionAttribute
    {
        public int Order { get; set; }
        public string TrueValue { get; set; }
        public ManufactureDescription(string description, int order, string trueValue = "") : base(description)
        {
            Order = order;
            TrueValue = trueValue;
        }
    }
}
