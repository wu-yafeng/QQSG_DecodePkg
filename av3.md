# .av3 文件结构描述

该文件描述的是一个渲染3D对象

## av3文件结构
下述的对文件的描述 格式 <行号>.<类型>:<名称> <注释>

1. int:type 类型
2. avt_descriptor:descriptor 描述信息
3~18. int[15]:AvtIds 炫装贴图id 共15个部位
19. 

## avt_descriptor 描述符

该行共8个字段

1. todo
2. Gender 性别限制
3. WeaponType 武器类型
4. todo
5. todo
6. todo
7. todo
8. RenderLevel 渲染的层级

## avt_part_desc

该行共2个字段

1. name
2. count 数量
