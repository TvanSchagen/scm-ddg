WITH RECURSIVE cte (
	q3, pid, cname, cid, oid, oname, lid, lcountry
)
AS
(
	SELECT parent.name, parent.id, child.name, child.id, organization.id, organization.name, location.id, location.country
	FROM consists_of
	JOIN product AS parent ON consists_of.parent_product_id = parent.id
	JOIN product AS child ON consists_of.child_product_id = child.id
	JOIN supplies ON consists_of.parent_product_id = supplies.product_id
	JOIN organization ON supplies.organization_id = organization.id
    JOIN location ON supplies.location_id = location.id
    WHERE organization.name = "Top Level Organization Blanda - Bode"

	UNION ALL

	SELECT parent.name, parent.id, child.name, child.id, organization.id, organization.name, location.id, location.country
	FROM consists_of
	JOIN product AS parent ON consists_of.parent_product_id = parent.id
	JOIN product AS child ON consists_of.child_product_id = child.id
	JOIN supplies ON consists_of.child_product_id = supplies.product_id
	JOIN organization ON supplies.organization_id = organization.id
    JOIN location ON supplies.location_id = location.id
	JOIN cte ON cte.cid = parent.id
)

SELECT q3, pid, cname, cid, oid, oname, lid, lcountry
FROM cte
WHERE cname LIKE "%concrete%"