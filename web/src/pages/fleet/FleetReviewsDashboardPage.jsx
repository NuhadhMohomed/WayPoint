import React, { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { fleetApi } from '@/features/fleet/fleetApi';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Loader2, Star, User } from 'lucide-react';
import { Alert, AlertDescription } from '@/components/ui/alert';

export default function FleetReviewsDashboardPage() {
  const { entityType, entityId } = useParams(); // entityType can be 'bus' or 'driver'
  const [reviews, setReviews] = useState([]);
  const [summary, setSummary] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [filter, setFilter] = useState({ pageNumber: 1, pageSize: 20 });

  useEffect(() => {
    loadData();
  }, [entityType, entityId, filter]);

  const loadData = async () => {
    try {
      setLoading(true);
      setError(null);
      if (entityType === 'bus') {
        const [revData, sumData] = await Promise.all([
          fleetApi.getBusReviews(entityId, filter),
          fleetApi.getBusRatingSummary(entityId)
        ]);
        setReviews(revData.items || []);
        setSummary(sumData);
      } else if (entityType === 'driver') {
        const [revData, sumData] = await Promise.all([
          fleetApi.getDriverReviews(entityId, filter),
          fleetApi.getDriverRatingSummary(entityId)
        ]);
        setReviews(revData.items || []);
        setSummary(sumData);
      } else {
        setError("Invalid entity type. Use 'bus' or 'driver'.");
      }
    } catch (err) {
      setError("Failed to load reviews data. " + (err.response?.data?.detail || err.message));
    } finally {
      setLoading(false);
    }
  };

  const renderStars = (rating) => {
    return (
      <div className="flex">
        {[1, 2, 3, 4, 5].map(star => (
          <Star 
            key={star} 
            className={`w-4 h-4 ${star <= rating ? 'text-amber-500 fill-amber-500' : 'text-slate-300'}`} 
          />
        ))}
      </div>
    );
  };

  return (
    <div className="p-6 max-w-6xl mx-auto">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-slate-900 mb-2 font-display">
          {entityType === 'bus' ? 'Bus' : 'Driver'} Reviews
        </h1>
        <p className="text-slate-500">
          View ratings and passenger feedback for {entityType === 'bus' ? summary?.registrationNumber : summary?.fullName}.
        </p>
      </div>

      {error && (
        <Alert variant="destructive" className="mb-6">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {loading && !summary ? (
        <div className="flex justify-center p-12">
          <Loader2 className="h-8 w-8 animate-spin text-blue-600" />
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          
          {/* Summary Panel */}
          <div className="md:col-span-1">
            <Card className="sticky top-6">
              <CardHeader>
                <CardTitle>Rating Summary</CardTitle>
              </CardHeader>
              <CardContent>
                {summary ? (
                  <div className="flex flex-col items-center">
                    <div className="text-5xl font-bold text-slate-900 mb-2">
                      {summary.averageRating.toFixed(1)}
                    </div>
                    {renderStars(Math.round(summary.averageRating))}
                    <div className="text-sm text-slate-500 mt-2">
                      Based on {summary.totalReviews} reviews
                    </div>

                    <div className="w-full mt-6 space-y-2">
                      {[5, 4, 3, 2, 1].map(star => {
                        const count = summary.ratingDistribution[star - 1] || 0;
                        const percentage = summary.totalReviews > 0 ? (count / summary.totalReviews) * 100 : 0;
                        return (
                          <div key={star} className="flex items-center text-sm">
                            <span className="w-8 text-slate-600">{star} ★</span>
                            <div className="flex-1 h-3 mx-2 bg-slate-100 rounded-full overflow-hidden">
                              <div 
                                className="h-full bg-amber-500 rounded-full"
                                style={{ width: `${percentage}%` }}
                              />
                            </div>
                            <span className="w-8 text-right text-slate-500">{count}</span>
                          </div>
                        );
                      })}
                    </div>
                  </div>
                ) : (
                  <p className="text-slate-500 text-center">No summary available.</p>
                )}
              </CardContent>
            </Card>
          </div>

          {/* Reviews List */}
          <div className="md:col-span-2 space-y-4">
            {reviews.length === 0 ? (
              <Card>
                <CardContent className="p-8 text-center text-slate-500">
                  No reviews found for this {entityType}.
                </CardContent>
              </Card>
            ) : (
              reviews.map(review => (
                <Card key={review.id} className="overflow-hidden">
                  <CardContent className="p-6">
                    <div className="flex justify-between items-start mb-4">
                      <div className="flex items-center gap-3">
                        <div className="bg-slate-100 p-2 rounded-full">
                          <User className="w-5 h-5 text-slate-500" />
                        </div>
                        <div>
                          <p className="font-semibold text-slate-900">
                            {review.passengerName}
                          </p>
                          <p className="text-xs text-slate-500">
                            {new Date(review.createdAt).toLocaleDateString()}
                          </p>
                        </div>
                      </div>
                      {renderStars(review.rating)}
                    </div>
                    {review.comment ? (
                      <p className="text-slate-700 leading-relaxed">
                        {review.comment}
                      </p>
                    ) : (
                      <p className="text-slate-400 italic">No comment provided.</p>
                    )}
                  </CardContent>
                </Card>
              ))
            )}

            <div className="flex justify-between items-center mt-6">
              <Button 
                variant="outline" 
                disabled={filter.pageNumber === 1 || loading}
                onClick={() => setFilter(prev => ({ ...prev, pageNumber: prev.pageNumber - 1 }))}
              >
                Previous
              </Button>
              <span className="text-sm text-slate-500">Page {filter.pageNumber}</span>
              <Button 
                variant="outline"
                disabled={reviews.length < filter.pageSize || loading}
                onClick={() => setFilter(prev => ({ ...prev, pageNumber: prev.pageNumber + 1 }))}
              >
                Next
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
