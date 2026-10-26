UPDATE media.sponsorblock_segments
SET locked = NOT locked
WHERE sponsorblock_segments.locked
   OR sponsorblock_segments.modified_at > (SELECT applied
                                           FROM schemaversions
                                           WHERE scriptname LIKE '%000032_locked-segments.sql');
