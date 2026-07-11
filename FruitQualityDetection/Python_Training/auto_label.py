"""
自动标注脚本
功能：用 COCO 预训练的 YOLO11n 模型自动为图片生成 YOLO 格式标签
适用：苹果、香蕉、橘子在 COCO 中已有，可直接识别

要求：
  - 已安装 ultralytics: pip install ultralytics
  - 图片已放在 dataset/images/train/ 和 dataset/images/val/
  - 输出标签到 dataset/labels/train/ 和 dataset/labels/val/
"""
import os
from pathlib import Path
from ultralytics import YOLO

# ==================== 配置区 ====================
CONF_THRESHOLD = 0.4          # 置信度阈值，低于此值的检测框丢弃
IOU_THRESHOLD = 0.45          # NMS IoU阈值

# COCO 原始类别ID → 你的项目类别ID 映射
# COCO: apple=47, banana=46, orange=49
# 你的项目: apple=0, banana=1, orange=2
COCO_TO_CUSTOM = {
    47: 0,   # apple
    46: 1,   # banana
    49: 2,   # orange
}
# ================================================

def auto_label_split(split_name):
    """为指定 split (train/val) 自动标注"""
    img_dir = Path("dataset/images") / split_name
    lbl_dir = Path("dataset/labels") / split_name
    lbl_dir.mkdir(parents=True, exist_ok=True)
    
    if not img_dir.exists():
        print(f"❌ 找不到图片目录: {img_dir}")
        return
    
    # 加载 COCO 预训练模型（首次会自动下载 yolo11n.pt）
    print(f"\n🔍 正在加载 YOLO11n 预训练模型...")
    model = YOLO("yolo11n.pt")
    
    img_exts = ('.jpg', '.jpeg', '.png', '.bmp', '.webp')
    images = [f for f in img_dir.iterdir() if f.suffix.lower() in img_exts]
    images.sort()
    
    print(f"📁 {split_name}: 共 {len(images)} 张图片，开始自动标注...")
    
    total_boxes = 0
    miss_count = 0
    
    for img_path in images:
        # 推理（关闭 verbose 减少输出）
        results = model(str(img_path), conf=CONF_THRESHOLD, iou=IOU_THRESHOLD, verbose=False)[0]
        
        h, w = results.orig_shape
        label_lines = []
        
        for box in results.boxes:
            cls_id = int(box.cls[0])
            conf = float(box.conf[0])
            
            # 只保留苹果、香蕉、橘子
            if cls_id not in COCO_TO_CUSTOM:
                continue
            
            custom_id = COCO_TO_CUSTOM[cls_id]
            x_center, y_center, bw, bh = box.xywhn[0].tolist()
            
            label_lines.append(f"{custom_id} {x_center:.6f} {y_center:.6f} {bw:.6f} {bh:.6f}")
            total_boxes += 1
        
        # 写入标签文件（与图片同名，.txt）
        out_file = lbl_dir / (img_path.stem + ".txt")
        with open(out_file, 'w', encoding='utf-8') as f:
            f.write('\n'.join(label_lines))
        
        if len(label_lines) == 0:
            miss_count += 1
            print(f"  ⚠️ {img_path.name}: 未检测到目标（需人工补标）")
        else:
            print(f"  ✅ {img_path.name}: {len(label_lines)} 个框")
    
    print(f"\n📊 {split_name} 完成: {len(images)} 张图, {total_boxes} 个框, {miss_count} 张未检出")

def main():
    for split in ["train", "val"]:
        auto_label_split(split)
    
    print("\n" + "="*50)
    print("🎉 自动标注全部完成！")
    print("⚠️  重要提示：")
    print("   1. 请用 LabelImg 等工具审核修正标签，尤其是未检出的图片")
    print("   2. 检查是否有误识别（如橘子被标成苹果）")
    print("   3. 确认无误后再开始训练！")
    print("="*50)

if __name__ == "__main__":
    main()