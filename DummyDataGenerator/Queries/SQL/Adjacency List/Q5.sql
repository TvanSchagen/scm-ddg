WITH RECURSIVE cte (
	q5, business_type, parent_pname, parent_pid, parent_oid, parent_oname, child_ean, child_pname, child_pid, child_oid, child_oname
)
AS
(
	SELECT parent.ean, CASE WHEN child_org.number_of_employees > 5000 THEN "Large" WHEN child_org.number_of_employees < 1000 THEN "Small" ELSE "Regular" END AS business_type,
		parent.name, parent.id, parent_org.id, parent_org.name, child.ean, child.name, child.id, child_org.id, child_org.name
	FROM consists_of
	JOIN product AS parent ON consists_of.parent_product_id = parent.id
	JOIN product AS child ON consists_of.child_product_id = child.id
	JOIN supplies AS parent_supplies ON consists_of.parent_product_id = parent_supplies.product_id
    JOIN supplies AS child_supplies ON consists_of.child_product_id = child_supplies.product_id
	JOIN organization AS parent_org ON parent_supplies.organization_id = parent_org.id
    JOIN organization AS child_org ON child_supplies.organization_id = child_org.id
	WHERE parent_org.name = "Top Level Organization Blanda - Bode"
    
	UNION ALL

	SELECT parent.ean, CASE WHEN child_org.number_of_employees > 5000 THEN "Large" WHEN child_org.number_of_employees < 1000 THEN "Small" ELSE "Regular" END AS business_type,
		parent.name, parent.id, parent_org.id, parent_org.name, child.ean, child.name, child.id, child_org.id, child_org.name
	FROM consists_of
	JOIN product AS parent ON consists_of.parent_product_id = parent.id
	JOIN product AS child ON consists_of.child_product_id = child.id
	JOIN supplies AS parent_supplies ON consists_of.parent_product_id = parent_supplies.product_id
    JOIN supplies AS child_supplies ON consists_of.child_product_id = child_supplies.product_id
	JOIN organization AS parent_org ON parent_supplies.organization_id = parent_org.id
    JOIN organization AS child_org ON child_supplies.organization_id = child_org.id
	JOIN cte ON cte.child_pid = parent.id
)

SELECT q5, parent_pname, parent_oname, child_ean, child_pname, child_oname, business_type
FROM cte