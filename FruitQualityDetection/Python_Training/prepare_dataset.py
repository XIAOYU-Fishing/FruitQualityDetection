"""
数据集准备脚本
功能：统一重命名、按数量划分训练集/验证集、自动生成 data.yaml

原始数据假设结构：
raw_data/
    apple_good/     ← 250张苹果（好）
    apple_bad/      ← 250张苹果（坏）
    banana_good/    ← 250张香蕉（好）
    banana_bad/     ← 250张香蕉（坏）
    orange_good/    ← 250张橘子（好）
    orange_bad/     ← 250张橘子（坏）

说明：YOLO 只负责检测"水果类别"（苹果/香蕉/橘子），
      "好/坏"由后续 Halcon 缺陷分析判定，不作为 YOLO 的类别。
"""
import os, shutil, random
from pathlib import Path
from collections import defaultdict

# ==================== 配置区 ====================
RAW_DIR = "raw_data"                    # 原始图片根目录
OUTPUT_DIR = "dataset"                  # 输出数据集目录
CLASS_MAP = {                           # 文件夹前缀 → YOLO类别ID
    "apple": 0,
    "banana": 1,
    "orange": 2,
}
TRAIN_TARGET = 1500                     # 训练集目标总数
VAL_TARGET = 600                        # 验证集目标总数
# ================================================

def prepare_dataset():
    random.seed(42)
    
    # 创建目录结构
    for split in ["train", "val"]:
        (Path(OUTPUT_DIR) / "images" / split).mkdir(parents=True, exist_ok=True)
        (Path(OUTPUT_DIR) / "labels" / split).mkdir(parents=True, exist_ok=True)
    
    # 收集所有图片并按类别分组
    class_images = defaultdict(list)
    
    raw_path = Path(RAW_DIR)
    if not raw_path.exists():
        print(f"❌ 错误：找不到原始数据目录 {RAW_DIR}")
        print("请按以下结构放置图片：")
        print("raw_data/apple_good/ 、 raw_data/apple_bad/ 、 raw_data/banana_good/ ...")
        return
    
    for folder in raw_path.iterdir():
        if not folder.is_dir():
            continue
        
        # 解析文件夹名：如 "apple_good" → class_name="apple", class_id=0
        folder_name = folder.name.lower()
        matched_class = None
        for cls_name, cls_id in CLASS_MAP.items():
            if folder_name.startswith(cls_name):
                matched_class = cls_id
                break
        
        if matched_class is None:
            print(f"⚠️ 跳过未知文件夹: {folder_name}")
            continue
        
        # 收集该文件夹下所有图片
        img_exts = ('.jpg', '.jpeg', '.png', '.bmp', '.webp')
        imgs = [f for f in folder.iterdir() if f.suffix.lower() in img_exts]
        class_images[matched_class].extend(imgs)
        print(f"  📁 {folder_name}: {len(imgs)} 张 → 类别 {matched_class}")
    
    # 检查各类数量
    for cls_id, imgs in sorted(class_images.items()):
        cls_name = [k for k,v in CLASS_MAP.items() if v==cls_id][0]
        print(f"  类别 {cls_id}({cls_name}): 共 {len(imgs)} 张")
    
    # 按类别划分 train/val，确保每类都按比例出现
    train_files = []
    val_files = []
    
    for cls_id, imgs in sorted(class_images.items()):
        random.shuffle(imgs)
        # 每类训练目标：1500/3 = 500张；验证目标：600/3 = 200张
        n_train = min(len(imgs), TRAIN_TARGET // len(CLASS_MAP))
        n_val = min(len(imgs) - n_train, VAL_TARGET // len(CLASS_MAP))
        
        train_files.extend([(f, cls_id) for f in imgs[:n_train]])
        val_files.extend([(f, cls_id) for f in imgs[n_train:n_train+n_val]])
    
    print(f"\n📊 划分结果: 训练集 {len(train_files)} 张, 验证集 {len(val_files)} 张")
    
    # 复制图片并生成空标签（如果用户已有标签，一并复制）
    def copy_split(split_name, file_list):
        count = 0
        for img_path, cls_id in file_list:
            # 统一重命名：class_id_序号.扩展名
            count += 1
            new_name = f"{cls_id}_{split_name}_{count:04d}{img_path.suffix.lower()}"
            dst_img = Path(OUTPUT_DIR) / "images" / split_name / new_name
            shutil.copy(img_path, dst_img)
            
            # 检查是否存在同名标签文件
            src_label = img_path.with_suffix(".txt")
            if src_label.exists():
                dst_label = Path(OUTPUT_DIR) / "labels" / split_name / (Path(new_name).with_suffix(".txt"))
                shutil.copy(src_label, dst_label)
        return count
    
    copy_split("train", train_files)
    copy_split("val", val_files)
    
    # 生成 data.yaml
    yaml_path = Path(OUTPUT_DIR).parent / "data.yaml"
    yaml_content = f"""# 自动生成的数据集配置文件
path: {Path(OUTPUT_DIR).absolute().as_posix()}  # 数据集绝对路径

train: images/train
val: images/val
# test: images/test  # 如需测试集可取消注释

# 类别数
nc: {len(CLASS_MAP)}

# 类别名称（顺序对应 class_id 0,1,2）
names:
  0: apple
  1: banana
  2: orange
"""
    with open(yaml_path, 'w', encoding='utf-8') as f:
        f.write(yaml_content)
    
    print(f"\n✅ 完成！")
    print(f"   数据集目录: {Path(OUTPUT_DIR).absolute()}")
    print(f"   配置文件: {yaml_path.absolute()}")
    print(f"\n下一步：")
    print(f"   1. 如果还没有标签，运行: python auto_label.py")
    print(f"   2. 确认 labels/train/ 和 labels/val/ 下有对应的 .txt 文件")
    print(f"   3. 运行: python train.py")

if __name__ == "__main__":
    prepare_dataset()