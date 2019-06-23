SELECT organization.name AS q8, organization.email_address, location.country, location.city, product.name, activity.name
FROM supplies
JOIN organization ON organization.id = supplies.organization_id
JOIN activity ON activity.id = supplies.activity_id
JOIN location ON location.id = supplies.location_id
JOIN product ON product.id = supplies.product_id
WHERE location.latitude > -40.0 
AND location.latitude < 44.0
AND location.longtitude > -30.0
AND location.longtitude < 70.0