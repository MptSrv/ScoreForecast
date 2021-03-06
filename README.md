# ScoreForecast
ТЕСТОВОЕ ЗАДАНИЕ для соискателя должности инженера-программиста
## Описание задачи
> Две условные команды «Хозяева» и «Гости» играют футбольный матч, который длится ровно
90 минут без компенсированного времени, экстра-таймов и серии пенальти. По данным предыдущих встреч
этих команд собрана статистика, приведенная в таблице 1. Написать алгоритм определения вероятностей
исходов заданного интервала голов внутри этого матча. Интервал определяется номерами первого
и последнего голов внутри матча.
## Входные данные
1. время, оставшееся до конца матча;
2. забитые на текущий момент голы в порядке их реализации, например: счёт 2:1, был реализован так:
забили хозяева, забили хозяева, забили гости;
3. номера голов начала и конца искомого интервала.
## Выходные данные
+ вероятность победы хозяев на заданном интервале голов;
+ вероятность победы гостей на заданном интервале голов;
+ вероятность ничьи; если количество голов в интервале нечётное, очевидно, что ничьи в таком
интервале быть не может;
+ вероятность что интервал не будет закрыт; например, ни один гол интервала [2..5] не будет забит, если
матч закончится со счётом 0:0.
# Решение
## Расчет вероятностной модели на основе распределения Пуассона
Голы в футбольном матче хорошо описываются распределением Пуассона, поэтому соответствующая формула может быть использована для вычисления вероятности голов 
обеих команд в различных комбинациях. При этом, в коэффициент формулы Пуассона (lambda) вносится необходимая поправка с учетом оставшегося времени и уже забитых голов.
## Проверка методом Монте-Карло
Для проверки используется метод Монте-Карло, заключающийся в прогоне большого количества тестовых данных, сгенерированных с учетом разной результативности команд.
