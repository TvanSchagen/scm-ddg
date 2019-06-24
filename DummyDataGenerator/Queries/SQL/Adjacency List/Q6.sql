WITH RECURSIVE cte (
	q6, parent_pname, parent_pid, parent_oid, parent_oname, parent_country, child_ean, child_pname, child_pid, child_oid, child_oname, child_country, depth
)
AS
(
	SELECT parent.ean, parent.name, parent.id, parent_org.id, parent_org.name, parent_loc.country, child.ean, child.name, child.id, child_org.id, child_org.name, child_loc.country, 1
	FROM consists_of
	JOIN product AS parent ON consists_of.parent_product_id = parent.id
	JOIN product AS child ON consists_of.child_product_id = child.id
	JOIN supplies AS parent_supplies ON consists_of.parent_product_id = parent_supplies.product_id
    JOIN supplies AS child_supplies ON consists_of.child_product_id = child_supplies.product_id
	JOIN organization AS parent_org ON parent_supplies.organization_id = parent_org.id
    JOIN organization AS child_org ON child_supplies.organization_id = child_org.id
    JOIN location AS parent_loc ON parent_supplies.location_id = parent_loc.id
    JOIN location AS child_loc ON child_supplies.location_id = child_loc.id
    
    WHERE parent.category = "Baby"
    AND parent.created > "2017-01-01 00:00:00"
    AND parent_org.number_of_employees > 1000
    AND parent_loc.country IN ("Guam", "Georgia", "Dominica")
    
	UNION ALL

	SELECT parent.ean, parent.name, parent.id, parent_org.id, parent_org.name, parent_loc.country, child.ean, child.name, child.id, child_org.id, child_org.name, child_loc.country, cte.depth + 1
	FROM consists_of
	JOIN product AS parent ON consists_of.parent_product_id = parent.id
	JOIN product AS child ON consists_of.child_product_id = child.id
	JOIN supplies AS parent_supplies ON consists_of.parent_product_id = parent_supplies.product_id
    JOIN supplies AS child_supplies ON consists_of.child_product_id = child_supplies.product_id
	JOIN organization AS parent_org ON parent_supplies.organization_id = parent_org.id
    JOIN organization AS child_org ON child_supplies.organization_id = child_org.id
	JOIN location AS parent_loc ON parent_supplies.location_id = parent_loc.id
    JOIN location AS child_loc ON child_supplies.location_id = child_loc.id
	JOIN cte ON cte.child_pid = parent.id
    WHERE depth < 4
)

SELECT q6, parent_pname, parent_oname, parent_country, child_ean, child_pname, child_oname, child_country
FROM cte