WITH RECURSIVE cte (
	q1, parent_product_id, child_product_name, child_product_id
)
AS
(
	SELECT parent.name, parent.id, child.name, child.id
	FROM consists_of
	JOIN product AS parent ON consists_of.parent_product_id = parent.id
	JOIN product AS child ON consists_of.child_product_id = child.id
	WHERE parent.name = "Top Level Product Awesome Steel Car"
	
    UNION ALL

	SELECT parent.name, parent.id, child.name, child.id
	FROM consists_of
	JOIN product AS parent ON consists_of.parent_product_id = parent.id
	JOIN product AS child ON consists_of.child_product_id = child.id
    JOIN cte ON cte.child_product_id = parent.id
)

SELECT q1, parent_product_id, child_product_name, child_product_id
FROM cte