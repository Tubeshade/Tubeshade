SELECT id,
       created_at,
       created_by_user_id,
       modified_at,
       modified_by_user_id,
       video_id,
       external_id,
       start_time,
       end_time,
       category,
       action,
       locked,
       description
FROM media.sponsorblock_segments
