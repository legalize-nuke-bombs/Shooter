package com.example.shooter.game.world;

import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;

import java.util.List;
import java.util.Set;
import java.util.UUID;

public interface WorldRepository extends JpaRepository<World, UUID> {
    @Query("SELECT w FROM World w WHERE (?1 IS NULL OR w.id in ?1) ORDER BY w.name, w.id")
    List<World> findByWorldIdsNameOrder(Set<UUID> worldIds, Pageable pageable);

    @Query("SELECT w FROM World w WHERE (?1 IS NULL OR w.id in ?1) ORDER BY w.createdAt DESC, w.id")
    List<World> findByWorldIdsCreatedAtOrder(Set<UUID> worldIds, Pageable pageable);

    @Query("SELECT w FROM World w WHERE (?1 IS NULL OR w.id in ?1) ORDER BY w.accessedAt DESC, w.id")
    List<World> findByWorldIdsAccessedAtOrder(Set<UUID> worldIds, Pageable pageable);
}
