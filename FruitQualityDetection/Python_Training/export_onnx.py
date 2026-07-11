"""
ONNX 导出脚本
将训练好的 PyTorch 模型 (.pt) 导出为 C# ONNX Runtime 可用的 .onnx 文件

导出参数说明：
  - format="onnx": 导出 ONNX 格式
  - imgsz=640: 固定输入尺寸（与 C# 端 YoloDetector.cs 一致）
  - dynamic=False: 关闭动态 batch，C# 端更简单
  - nms=False: 不在 ONNX 中嵌入 NMS（C# 代码已手动实现）
  - simplify=True: 简化模型结构，减少算子
  - opset=12: ONNX 算子集版本（兼容性最好）
  - batch=1: 固定 batch size
"""
from ultralytics import YOLO
from pathlib import Path

# ==================== 配置区 ====================
MODEL_PATH = "runs/fruit_train/weights/best.pt"   # 训练好的模型
OUTPUT_DIR = "runs/fruit_train/weights/"            # 输出目录（默认同目录）
# ================================================

def main():
    print("="*60)
    print("📦 ONNX 模型导出")
    print("="*60)
    
    if not Path(MODEL_PATH).exists():
        print(f"❌ 找不到模型文件: {MODEL_PATH}")
        print("请确认训练已完成，或修改 MODEL_PATH")
        return
    
    print(f"\n📂 加载模型: {MODEL_PATH}")
    model = YOLO(MODEL_PATH)
    
    print("🔧 开始导出 ONNX（约需 30~60 秒）...")
    model.export(
        format="onnx",
        imgsz=640,
        dynamic=False,
        nms=False,
        simplify=True,
        opset=12,
        batch=1
    )
    
    onnx_path = Path(OUTPUT_DIR) / "best.onnx"
    print(f"\n✅ 导出完成！")
    print(f"   ONNX 文件: {onnx_path.absolute()}")
    print(f"\n📋 下一步：")
    print(f"   1. 复制 best.onnx 到 C# 项目的 models/ 目录")
    print(f"   2. 运行 C# 程序，点击【加载模型】选择该文件")
    print(f"\n⚠️  注意：")
    print(f"   - 此 ONNX 不包含 NMS，C# 端 YoloDetector.cs 已处理")
    print(f"   - 输入尺寸固定为 640x640，与 C# 预处理一致")

if __name__ == "__main__":
    main()