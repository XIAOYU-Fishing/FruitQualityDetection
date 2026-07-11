"""
训练结果可视化脚本
功能：读取 results.csv，绘制 loss、learning rate、mAP、Precision、Recall 等关键图表

输入：runs/fruit_train/results.csv（训练时自动生成）
输出：training_curves.png（综合图表）
"""
import pandas as pd
import matplotlib.pyplot as plt
import numpy as np
from pathlib import Path

# ==================== 配置区 ====================
RESULTS_CSV = "runs/fruit_train/results.csv"   # 训练结果文件
OUTPUT_PNG = "training_curves.png"             # 输出图表文件名
DPI = 300                                       # 图片分辨率
# ================================================

def load_results(csv_path):
    """加载训练结果 CSV"""
    if not Path(csv_path).exists():
        print(f"❌ 找不到结果文件: {csv_path}")
        print("请确认训练已完成，或修改 RESULTS_CSV 路径")
        return None
    
    df = pd.read_csv(csv_path)
    # 清理列名（去除空格）
    df.columns = [c.strip() for c in df.columns]
    print(f"✅ 加载成功: {csv_path}")
    print(f"   共 {len(df)} 轮数据，列: {list(df.columns)}")
    return df

def plot_curves(df):
    """绘制训练曲线"""
    epochs = df["epoch"].values if "epoch" in df.columns else np.arange(len(df))
    
    # 创建 2x3 子图布局
    fig, axes = plt.subplots(2, 3, figsize=(18, 10), dpi=DPI)
    fig.suptitle("YOLO11 Fruit Detection Training Curves", fontsize=16, fontweight="bold")
    
    # 1. Box Loss (边界框回归损失)
    ax = axes[0, 0]
    if "train/box_loss" in df.columns:
        ax.plot(epochs, df["train/box_loss"], label="train/box_loss", color="blue", linewidth=1.5)
    if "val/box_loss" in df.columns:
        ax.plot(epochs, df["val/box_loss"], label="val/box_loss", color="orange", linewidth=1.5, linestyle="--")
    ax.set_title("Box Loss (边界框损失)", fontsize=12)
    ax.set_xlabel("Epoch")
    ax.set_ylabel("Loss")
    ax.legend()
    ax.grid(True, alpha=0.3)
    
    # 2. Classification Loss (分类损失)
    ax = axes[0, 1]
    if "train/cls_loss" in df.columns:
        ax.plot(epochs, df["train/cls_loss"], label="train/cls_loss", color="green", linewidth=1.5)
    if "val/cls_loss" in df.columns:
        ax.plot(epochs, df["val/cls_loss"], label="val/cls_loss", color="red", linewidth=1.5, linestyle="--")
    ax.set_title("Classification Loss (分类损失)", fontsize=12)
    ax.set_xlabel("Epoch")
    ax.set_ylabel("Loss")
    ax.legend()
    ax.grid(True, alpha=0.3)
    
    # 3. DFL Loss (Distribution Focal Loss，YOLOv8/11特有)
    ax = axes[0, 2]
    if "train/dfl_loss" in df.columns:
        ax.plot(epochs, df["train/dfl_loss"], label="train/dfl_loss", color="purple", linewidth=1.5)
    if "val/dfl_loss" in df.columns:
        ax.plot(epochs, df["val/dfl_loss"], label="val/dfl_loss", color="brown", linewidth=1.5, linestyle="--")
    ax.set_title("DFL Loss (分布焦点损失)", fontsize=12)
    ax.set_xlabel("Epoch")
    ax.set_ylabel("Loss")
    ax.legend()
    ax.grid(True, alpha=0.3)
    
    # 4. Learning Rate (学习率曲线)
    ax = axes[1, 0]
    lr_col = None
    for col in ["lr/pg0", "lr0", "lr", "x/lr"]:
        if col in df.columns:
            lr_col = col
            break
    if lr_col:
        ax.plot(epochs, df[lr_col], label=lr_col, color="crimson", linewidth=2)
        ax.set_title("Learning Rate (学习率)", fontsize=12)
        ax.set_xlabel("Epoch")
        ax.set_ylabel("LR")
        ax.legend()
        ax.grid(True, alpha=0.3)
    else:
        ax.text(0.5, 0.5, "No LR data", ha="center", va="center", transform=ax.transAxes)
    
    # 5. mAP Metrics (平均精度)
    ax = axes[1, 1]
    if "metrics/mAP50(B)" in df.columns:
        ax.plot(epochs, df["metrics/mAP50(B)"], label="mAP@50", color="darkgreen", linewidth=2)
    if "metrics/mAP50-95(B)" in df.columns:
        ax.plot(epochs, df["metrics/mAP50-95(B)"], label="mAP@50-95", color="navy", linewidth=2)
    ax.set_title("mAP (Mean Average Precision)", fontsize=12)
    ax.set_xlabel("Epoch")
    ax.set_ylabel("mAP")
    ax.legend()
    ax.grid(True, alpha=0.3)
    ax.set_ylim(0, 1.05)
    
    # 6. Precision & Recall (精确率与召回率)
    ax = axes[1, 2]
    if "metrics/precision(B)" in df.columns:
        ax.plot(epochs, df["metrics/precision(B)"], label="Precision", color="teal", linewidth=2)
    if "metrics/recall(B)" in df.columns:
        ax.plot(epochs, df["metrics/recall(B)"], label="Recall", color="coral", linewidth=2)
    ax.set_title("Precision & Recall", fontsize=12)
    ax.set_xlabel("Epoch")
    ax.set_ylabel("Value")
    ax.legend()
    ax.grid(True, alpha=0.3)
    ax.set_ylim(0, 1.05)
    
    plt.tight_layout(rect=[0, 0, 1, 0.96])
    plt.savefig(OUTPUT_PNG, dpi=DPI, bbox_inches="tight")
    plt.show()
    
    print(f"\n✅ 图表已保存: {Path(OUTPUT_PNG).absolute()}")
    
    # 打印最终指标摘要
    print("\n" + "="*50)
    print("📊 最终训练指标摘要（最后一轮）")
    print("="*50)
    last = df.iloc[-1]
    for col in df.columns:
        if col != "epoch":
            print(f"   {col}: {last[col]:.4f}")

def main():
    print("="*60)
    print("📈 YOLO11 训练结果可视化")
    print("="*60)
    
    df = load_results(RESULTS_CSV)
    if df is not None:
        plot_curves(df)

if __name__ == "__main__":
    main()