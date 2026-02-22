using System;
using System.Linq;
using System.Reflection;
using Duckov.Modding;
using ItemStatsSystem.Items;
using UnityEngine;
using Duckov.Utilities;
using ItemStatsSystem;

namespace MilitaryBackpackULTRA
{
  public class ModBehaviour : Duckov.Modding.ModBehaviour
  {
    protected override void OnAfterSetup()
    {
      Debug.Log("【行军背包ULTRA】开始加载！");

      // 创建行军背包ULTRA的配置
      var config = new ItemConfig
      {
        OriginalItemId = 40,
        NewItemId = 100006,
        DisplayName = "行军背包ULTRA",
        LocalizationKey = "militarybackpack_ultra",
        LocalizationDescValue = "终极行军背包，拥有60格超大容量和200负重！让你满载而归！",
        Weight = 1.0f,
        Value = 1000000,
        SlotCount = 6,
        Quality = 5,
        Weightadd = 200,
        Tags = new string[]
        {
          "Backpack",
          "Equipment",
          "Unique"
        }
      };

      CreateUltraBackpack(config);
      Debug.Log("【行军背包ULTRA】加载完成！");
    }

    // ==================================================

    private void CreateUltraBackpack(ItemConfig config)
    {
      Debug.Log($"【行军背包ULTRA】开始创建物品 {config.DisplayName}");

      // 1. 获取原始物品模板
      Item prefab = ItemAssetsCollection.GetPrefab(config.OriginalItemId);

      // 2. 克隆物品
      GameObject clonedGO = Instantiate(prefab.gameObject);
      clonedGO.name = $"NewItem_{config.NewItemId}";
      DontDestroyOnLoad(clonedGO);

      // 3. 获取物品组件
      Item newItem = clonedGO.GetComponent<Item>();

      // 4. 设置物品属性
      SetItemProperties(newItem, config);

      // 5. 配置背包槽位
      ConfigureSlots(newItem, config.SlotCount);

      // 6. 设置负重加成
      ReplaceModifiersWithCustom(newItem, config.Weightadd);

      // 7. 设置图标（沿用原物品的图标，或者你可以自己准备）
      SetItemIcon(newItem, prefab);

      // 8. 注册到游戏
      RegisterItem(newItem, config.NewItemId, clonedGO);

      Debug.Log($"【行军背包ULTRA】物品 {config.DisplayName} 创建成功！ID: {config.NewItemId}");
    }

    private void SetItemProperties(Item item, ItemConfig config)
    {
      // 使用反射设置各种属性
      SetFieldViaReflection(item, "typeID", config.NewItemId);
      SetFieldViaReflection(item, "weight", config.Weight);
      SetFieldViaReflection(item, "value", config.Value);
      SetFieldViaReflection(item, "displayName", config.LocalizationKey);
      SetFieldViaReflection(item, "quality", config.Quality);
      SetFieldViaReflection(item, "order", 0);

      // 清空原有标签，添加新标签
      item.Tags.Clear();
      foreach (string tagName in config.Tags)
      {
        Tag tag = GetTargetTag(tagName);
        if (tag != null)
        {
          item.Tags.Add(tag);
        }
      }
    }

    private Tag GetTargetTag(string tagName)
    {
      return Resources.FindObjectsOfTypeAll<Tag>().FirstOrDefault(t => t.name == tagName);
    }

    private void ConfigureSlots(Item item, int slotCount)
    {
      Item templateItem = ItemAssetsCollection.GetPrefab(1255);
      Slot templateSlot = templateItem.Slots[0];

      // 清空原有槽位
      item.Slots.Clear();

      // 创建新槽位
      for (int i = 0; i < slotCount; i++)
      {
        Slot newSlot = new Slot(templateSlot.Key);
        newSlot.SlotIcon = templateSlot.SlotIcon;

        // 设置唯一键
        FieldInfo keyField = typeof(Slot).GetField("key", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (keyField != null)
        {
          keyField.SetValue(newSlot, $"UltraSlot_{i}");
        }

        item.Slots.Add(newSlot);
      }

      Debug.Log($"【行军背包ULTRA】已配置 {slotCount} 个槽位");
    }

    private void SetItemIcon(Item newItem, Item original)
    {
      FieldInfo iconField = typeof(Item).GetField("icon", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
      if (iconField != null) { iconField.SetValue(newItem, original.Icon); }
    }

    private void ReplaceModifiersWithCustom(Item backpack, int weightBonus)
    {
      if (backpack.Modifiers == null) { return; }

      // 清空原有修饰符
      backpack.Modifiers.Clear();
      Debug.Log("【行军背包ULTRA】已清空原有修饰符");

      // 添加负重加成修饰符
      // ModifierDescription参数说明：
      // 参数1: 修饰符类型(2可能是属性加成)
      // 参数2: 目标属性("MaxWeight"代表最大负重)
      // 参数3: 未知参数(0)
      // 参数4: 加成值
      // 参数5: 是否为百分比(false表示固定值)
      // 参数6: 未知参数(0)
      ModifierDescription weightMod = new ModifierDescription(new ModifierTarget(), "MaxWeight", 0, (float)weightBonus, false, 0);

      backpack.Modifiers.Add(weightMod);
      Debug.Log($"【行军背包ULTRA】添加负重加成: +{weightBonus}");

      // 重新应用修饰符
      backpack.Modifiers.ReapplyModifiers();
      Debug.Log("【行军背包ULTRA】修饰符已应用");
    }

    private void SetFieldViaReflection(object obj, string fieldName, object value)
    {
      try
      {
        FieldInfo field = obj.GetType().GetField(fieldName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (field != null)
        {
          field.SetValue(obj, value);
        }
        else
        {
          // 尝试属性
          PropertyInfo prop = obj.GetType().GetProperty(fieldName,
              BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
          if (prop != null)
          {
            prop.SetValue(obj, value);
          }
          else
          {
            Debug.LogWarning($"【行军背包ULTRA】字段/属性 '{fieldName}' 未找到");
          }
        }
      }
      catch (Exception ex)
      {
        Debug.LogError($"【行军背包ULTRA】反射设置 {fieldName} 失败: {ex.Message}");
      }
    }

    private void RegisterItem(Item item, int itemId, GameObject clonedGO)
    {
      if (ItemAssetsCollection.AddDynamicEntry(item))
      {
        Debug.Log($"【行军背包ULTRA】成功注册新物品！ID: {itemId}");
      }
      else
      {
        Debug.LogError("【行军背包ULTRA】注册失败！");
        Destroy(clonedGO);
      }
    }

    // ItemConfig结构体
    private struct ItemConfig
    {
      public int OriginalItemId;      // 原始物品ID（作为模板）
      public int NewItemId;            // 新物品ID
      public string DisplayName;       // 显示名称
      public string LocalizationKey;   // 本地化键
      public string LocalizationDescValue; // 描述文本
      public float Weight;              // 物品重量
      public int Value;                 // 物品价值
      public int SlotCount;              // 背包格子数
      public int Quality;                // 品质等级
      public int Weightadd;              // 负重加成
      public string[] Tags;              // 物品标签

      public string LocalizationDesc => LocalizationKey + "_Desc";
    }
  }
}