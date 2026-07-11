"""
YOLO11 水果检测训练脚本
针对 RTX 4060 (8GB) 优化，1500张训练图/600张验证图，目标训练时间约2小时

参数设计逻辑：
  - epochs=100: 1500张图，batch=8，每轮约 1500/8=188 iterations，每 iteration ~0.4s
    每轮约 75s，100轮约 125分钟（2小时出头）
  - batch=8: RTX 4060 8GB 在 640x640 下可稳定运行
  - patience=20: 早停，连续20轮无提升则停止，防止过拟合
  - amp=True: 混合精度训练，提速30%
  - lr0=0.01: 初始学习率（YOLO默认）
  - lrf=0.01: 最终学习率 = lr0 * lrf = 0.0001，余弦退火
"""
from ultralytics import YOLO
import torch

def check_gpu():
    """检查 GPU 可用性"""
    if torch.cuda.is_available():
        device_name = torch.cuda.get_device_name(0)
        mem = torch.cuda.get_device_properties(0).total_memory / 1e9
        print(f"✅ GPU 可用: {device_name} ({mem:.1f} GB)")
        return True
    else:
        print("❌ GPU 不可用，将使用 CPU 训练（极慢！）")
        return False

def main():
    print("="*60)
    print("🚀 YOLO11 水果检测训练启动")
    print("="*60)
    
    check_gpu()
    
    # 加载预训练模型（会自动下载 yolo11n.pt）
    print("\n📦 加载预训练模型 yolo11n.pt...")
    model = YOLO("yolo11n.pt")
    
    # 开始训练
    print("\n🔥 开始训练（预计 2 小时左右）...")
    print("   按 Ctrl+C 可中断，已训练权重会自动保存\n")
    
    model.train(
        data="data.yaml",           # 数据集配置文件
        epochs=100,                 # 总轮数（约2小时）
        imgsz=640,                  # 输入图像尺寸
        batch=8,                    # 批次大小（RTX 4060 8GB推荐）
        device=0,                   # GPU 设备号
        workers=4,                  # 数据加载线程数（建议=CPU核心数或4）
        amp=True,                   # 自动混合精度（提速+省显存）
        patience=20,                # 早停耐心值：20轮无提升则停止
        project="runs",             # 训练结果保存目录
        name="fruit_train",         # 本次训练名称
        exist_ok=True,              # 允许覆盖同名项目
        
        # 优化器与学习率
        optimizer="AdamW",          # 优化器（AdamW 比 SGD 收敛更快）
        lr0=0.01,                   # 初始学习率
        lrf=0.01,                   # 最终学习率 = lr0 * lrf = 0.0001
        momentum=0.937,             # SGD 动量（AdamW 下为 betas 的一部分）
        weight_decay=0.0005,        # 权重衰减（正则化）
        
        # 数据增强（YOLO 自动处理，一般无需修改）
        hsv_h=0.015,                # HSV 色调增强幅度
        hsv_s=0.7,                  # HSV 饱和度增强幅度
        hsv_v=0.4,                  # HSV 亮度增强幅度
        degrees=0.0,                # 旋转角度（水果不建议大幅旋转）
        translate=0.1,              # 平移比例
        scale=0.5,                  # 缩放比例
        shear=0.0,                  # 剪切角度
        flipud=0.0,                 # 上下翻转概率
        fliplr=0.5,                 # 左右翻转概率
        mosaic=1.0,                 # Mosaic 增强概率（4图拼接）
        mixup=0.0,                  # MixUp 增强（小数据集不建议）
        
        # 保存与日志
        save=True,                  # 保存检查点
        save_period=10,             # 每10轮保存一次
        plots=True,                 # 自动生成训练曲线图
        verbose=True,               # 打印详细日志
    )
    
    print("\n" + "="*60)
    print("🎉 训练完成！")
    print("="*60)
    print(f"\n📁 最佳模型权重: runs/fruit_train/weights/best.pt")
    print(f"📁 最新模型权重: runs/fruit_train/weights/last.pt")
    print(f"📁 训练日志: runs/fruit_train/results.csv")
    print(f"\n下一步：")
    print(f"   1. 运行 python plot_training.py 查看详细图表")
    print(f"   2. 运行 python export_onnx.py 导出 C# 可用模型")

if __name__ == "__main__":
    main()