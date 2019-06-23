WITH RECURSIVE cte (
	parent_pname, parent_pid, parent_oid, parent_oname, child_pname, child_pid, child_oid, q4, depth
)
AS
(
	SELECT parent.name, parent.id, parent_org.id, parent_org.name, child.name, child.id, child_org.id, child_org.name, 1
	FROM consists_of
	JOIN product AS parent ON consists_of.parent_product_id = parent.id
	JOIN product AS child ON consists_of.child_product_id = child.id
	JOIN supplies AS parent_supplies ON consists_of.parent_product_id = parent_supplies.product_id
    JOIN supplies AS child_supplies ON consists_of.child_product_id = child_supplies.product_id
	JOIN organization AS parent_org ON parent_supplies.organization_id = parent_org.id
    JOIN organization AS child_org ON child_supplies.organization_id = child_org.id
    WHERE parent_org.name = "Top Level Organization Blanda - Bode"

	UNION ALL

	SELECT parent.name, parent.id, parent_org.id, parent_org.name, child.name, child.id, child_org.id, child_org.name, cte.depth + 1
	FROM consists_of
	JOIN product AS parent ON consists_of.parent_product_id = parent.id
	JOIN product AS child ON consists_of.child_product_id = child.id
	JOIN supplies AS parent_supplies ON consists_of.parent_product_id = parent_supplies.product_id
    JOIN supplies AS child_supplies ON consists_of.child_product_id = child_supplies.product_id
	JOIN organization AS parent_org ON parent_supplies.organization_id = parent_org.id
    JOIN organization AS child_org ON child_supplies.organization_id = child_org.id
	JOIN cte ON cte.child_pid = parent.id
)

SELECT distinct q4, depth
FROM cte
ORDER BY depth ASC