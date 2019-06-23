WITH RECURSIVE cte (
	q2, parent_product_id, child_product_name, child_product_id, depth
)
AS
(
	SELECT parent.name, parent.id, child.name, child.id, 1
	FROM consists_of
	JOIN product AS parent ON consists_of.parent_product_id = parent.id
	JOIN product AS child ON consists_of.child_product_id = child.id
	WHERE parent.name = "Top Level Product Awesome Steel Car"
	
    UNION ALL

	SELECT parent.name, parent.id, child.name, child.id, cte.depth + 1
	FROM consists_of
	JOIN product AS parent ON consists_of.parent_product_id = parent.id
	JOIN product AS child ON consists_of.child_product_id = child.id
    JOIN cte ON cte.child_product_id = parent.id
    WHERE depth < 5
)

SELECT q2, parent_product_id, child_product_name, child_product_id
FROM cte